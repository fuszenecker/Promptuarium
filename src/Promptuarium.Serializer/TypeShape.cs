using System.Collections.Concurrent;
using System.Reflection;

namespace Promptuarium.Serializer;

/// <summary>
/// A property as the serializer sees it: its name in the file, the type code it is declared to be stored as (from
/// <see cref="PromptuariumTypeAttribute"/>), its comment (<see cref="PromptuariumCommentAttribute"/>), whether it is
/// declared as Utf8String but is not a text of its own, or as Blob but is not a <c>byte[]</c> (so it is stored by a
/// conversion, see <see cref="PromptuariumSerializerOptions.ConversionForProperty"/>), and whether it can be set.
/// </summary>
internal sealed record ShapeMember(string Name, PropertyInfo Property, byte? DeclaredType, string? Comment, bool DeclaredConversion)
{
    public bool CanSet => Property.SetMethod is { IsPublic: true };

    /// <summary>The type of the property, without <see cref="Nullable{T}"/>.</summary>
    public Type ValueType => Nullable.GetUnderlyingType(Property.PropertyType) ?? Property.PropertyType;

    /// <summary>The conversion of a member declared as Utf8String or Blob (<see cref="DeclaredConversion"/>), or null.</summary>
    public ValueConversion? Conversion(PromptuariumSerializerOptions options) => DeclaredConversion ? options.ConversionForProperty(this) : null;

    /// <summary>Whether a property of <paramref name="type"/> declared as <paramref name="declared"/> is stored by a conversion.</summary>
    public static bool IsDeclaredConversion(PromptuariumType? declared, Type type)
    {
        Type value = Nullable.GetUnderlyingType(type) ?? type;

        return declared switch
        {
            PromptuariumType.Utf8String => !ValueConversion.IsTextOfItsOwn(value),
            PromptuariumType.Blob => value != typeof(byte[]) && value != typeof(object),
            _ => false,
        };
    }
}

/// <summary>
/// The members and the constructor of an object type, worked out once per type and name comparison. The members are
/// the public instance properties with a public getter (no indexers, no <see cref="PromptuariumIgnoreAttribute"/>),
/// in declaration order.
/// </summary>
internal sealed class TypeShape
{
    private static readonly ConcurrentDictionary<(Type, bool), TypeShape> Cache = new();

    private TypeShape(Type type, StringComparer comparer)
    {
        Members = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.GetMethod is { IsPublic: true } && property.GetIndexParameters().Length == 0)
            .Where(property => property.GetCustomAttribute<PromptuariumIgnoreAttribute>() is null)
            .Select(property => (Property: property, Declared: property.GetCustomAttribute<PromptuariumTypeAttribute>()?.Type))
            .Select(pair => new ShapeMember(
                pair.Property.GetCustomAttribute<PromptuariumNameAttribute>()?.Name ?? pair.Property.Name,
                pair.Property,
                (byte?)pair.Declared,
                pair.Property.GetCustomAttribute<PromptuariumCommentAttribute>()?.Text,
                ShapeMember.IsDeclaredConversion(pair.Declared, pair.Property.PropertyType)))
            .ToList();

        // Several names that differ only in case: the first one is found when reading
        ByName = new Dictionary<string, ShapeMember>(comparer);

        foreach (ShapeMember member in Members)
        {
            ByName.TryAdd(member.Name, member);
        }

        DefaultConstructor = type.IsValueType ? null : type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, Type.EmptyTypes);
        HasDefaultConstructor = type.IsValueType || DefaultConstructor is not null;

        // Without a parameterless constructor (e.g. a positional record): the public constructor with the most
        // parameters whose names are all member names
        if (!HasDefaultConstructor)
        {
            BindingConstructor = type.GetConstructors()
                .Where(constructor => constructor.GetParameters().All(parameter => parameter.Name is not null && ByName.ContainsKey(parameter.Name)))
                .OrderByDescending(constructor => constructor.GetParameters().Length)
                .FirstOrDefault();
        }
    }

    public IReadOnlyList<ShapeMember> Members { get; }

    public Dictionary<string, ShapeMember> ByName { get; }

    public ConstructorInfo? DefaultConstructor { get; }

    public bool HasDefaultConstructor { get; }

    public ConstructorInfo? BindingConstructor { get; }

    public static TypeShape Of(Type type, PromptuariumSerializerOptions options)
    {
        return Cache.GetOrAdd((type, options.PropertyNameCaseInsensitive), key => new TypeShape(key.Item1, options.NameComparer));
    }
}
