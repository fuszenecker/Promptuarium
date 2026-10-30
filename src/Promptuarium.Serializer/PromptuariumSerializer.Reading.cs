using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace Promptuarium.Serializer;

public static partial class PromptuariumSerializer
{
    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <summary>Reads a <typeparamref name="T"/> (an object or a string-keyed dictionary) from bytes.</summary>
    /// <exception cref="InvalidDataException">The bytes are not a valid file, or do not fit <typeparamref name="T"/>.</exception>
    public static T Deserialize<T>(ReadOnlyMemory<byte> bytes, PromptuariumSerializerOptions? options = null)
    {
        options ??= PromptuariumSerializerOptions.Default;
        return Deserialize<T>(Load(bytes, options), options);
    }

    /// <summary>Reads a <typeparamref name="T"/> (an object or a string-keyed dictionary) from a tree.</summary>
    /// <exception cref="InvalidDataException">The tree is not a valid file, or does not fit <typeparamref name="T"/>.</exception>
    public static T Deserialize<T>(Element root, PromptuariumSerializerOptions? options = null)
    {
        return (T)Deserialize(root, typeof(T), options);
    }

    /// <summary>Reads a value of <paramref name="type"/> (an object or a string-keyed dictionary) from a tree.</summary>
    /// <exception cref="InvalidDataException">The tree is not a valid file, or does not fit <paramref name="type"/>.</exception>
    public static object Deserialize(Element root, Type type, PromptuariumSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(type);
        options ??= PromptuariumSerializerOptions.Default;
        return ReadObject(RootMembers(root, options), type, existing: null, 1, options);
    }

    /// <summary>Reads a <typeparamref name="T"/> (an object or a string-keyed dictionary) from a stream.</summary>
    /// <exception cref="InvalidDataException">The stream does not hold a valid file, or it does not fit <typeparamref name="T"/>.</exception>
    public static async Task<T> DeserializeAsync<T>(Stream stream, PromptuariumSerializerOptions? options = null, CancellationToken cancellationToken = default)
    {
        options ??= PromptuariumSerializerOptions.Default;
        Element root;

        try
        {
            root = await Element.LoadAsync(stream, options.LoadOptions, cancellationToken).ConfigureAwait(false);
        }
        catch (PromptuariumException exception)
        {
            throw new InvalidDataException("The stream does not hold a Promptuarium tree.", exception);
        }

        return Deserialize<T>(root, options);
    }

    /// <summary>
    /// Applies a file onto <paramref name="target"/>: the members in the file overwrite the ones of the target, nested
    /// objects and dictionaries are applied member by member (a get-only one too), arrays are replaced, and a NULL
    /// member leaves the current value as it is. Layering e.g. the user's settings on top of the defaults is a
    /// <see cref="Deserialize{T}(ReadOnlyMemory{byte}, PromptuariumSerializerOptions?)"/> and a
    /// <see cref="Populate(ReadOnlyMemory{byte}, object, PromptuariumSerializerOptions?)"/> for each layer.
    /// </summary>
    /// <exception cref="InvalidDataException">The bytes are not a valid file, or do not fit the target.</exception>
    public static void Populate(ReadOnlyMemory<byte> bytes, object target, PromptuariumSerializerOptions? options = null)
    {
        options ??= PromptuariumSerializerOptions.Default;
        Populate(Load(bytes, options), target, options);
    }

    /// <inheritdoc cref="Populate(ReadOnlyMemory{byte}, object, PromptuariumSerializerOptions?)"/>
    public static void Populate(Element root, object target, PromptuariumSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(target);

        if (target.GetType().IsValueType)
        {
            throw new ArgumentException("A value type can not be populated in place.", nameof(target));
        }

        options ??= PromptuariumSerializerOptions.Default;
        ReadObject(RootMembers(root, options), target.GetType(), target, 1, options);
    }

    private static Element Load(ReadOnlyMemory<byte> bytes, PromptuariumSerializerOptions options)
    {
        try
        {
            return Element.FromBytes(bytes, options.LoadOptions);
        }
        catch (PromptuariumException exception)
        {
            throw new InvalidDataException("The bytes are not a Promptuarium tree.", exception);
        }
    }

    private static List<Element> RootMembers(Element root, PromptuariumSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(root);

        if (root.Data.IsEmpty && root.MetaData.Span.SequenceEqual(options.SignatureBytes))
        {
            return [.. root.Children];
        }

        return (options.UnexpectedSignature?.Invoke(Encoding.Latin1.GetString(root.MetaData.Span)) ?? SignatureAction.Reject) switch
        {
            SignatureAction.Accept => [.. root.Children],
            SignatureAction.Skip => [],
            _ => throw new InvalidDataException($"The file does not have the signature \"{options.Signature}\"."),
        };
    }

    // ---------------------------------------------------------------- holders

    private static object? ReadHolder(Element holder, Type type, object? existing, int depth, PromptuariumSerializerOptions options)
    {
        if (depth > options.MaxDepth)
        {
            throw new InvalidDataException($"The file is nested deeper than {options.MaxDepth} levels.");
        }

        IReadOnlyList<Element> children = holder.Children;

        if (children.Count == 0)
        {
            throw new InvalidDataException("A holder has no TYPE node.");
        }

        Element typeNode = children[0];
        RequireTag(typeNode, CfgFormat.Type, "the first child of a holder");
        RequireLeaf(typeNode);

        if (typeNode.Data.Length != 1)
        {
            throw new InvalidDataException("A TYPE node holds exactly one byte.");
        }

        Type target = Nullable.GetUnderlyingType(type) ?? type;

        switch (typeNode.Data.Span[0])
        {
            case CfgFormat.Null:
                RequireChildCount(holder, 1);
                return existing;

            case CfgFormat.Object:
                return ReadObject([.. children.Skip(1)], target, existing, depth, options);

            case CfgFormat.Array:
                return ReadArray([.. children.Skip(1)], target, depth, options);

            case CfgFormat.Boolean:
                ReadOnlyMemory<byte> flag = ScalarValue(holder);

                if (flag.Length != 1 || flag.Span[0] > 1)
                {
                    throw new InvalidDataException("A BOOLEAN value is 0x00 or 0x01.");
                }

                return Fit(flag.Span[0] == 1, target);

            case CfgFormat.Float64:
                ReadOnlyMemory<byte> raw = ScalarValue(holder);

                if (raw.Length != sizeof(double))
                {
                    throw new InvalidDataException("A FLOAT64 value is 8 bytes.");
                }

                double number = raw.AsDouble();

                if (!double.IsFinite(number))
                {
                    throw new InvalidDataException("A FLOAT64 value is finite.");
                }

                return FitNumber(number, target);

            case CfgFormat.Utf8String:
                return FitText(DecodeUtf8(ScalarValue(holder)), target);

            case CfgFormat.Blob:
                return Fit(ScalarValue(holder).ToArray(), target);

            case byte unknown:
                throw new InvalidDataException($"Unsupported type code 0x{unknown:x2}.");
        }
    }

    // ---------------------------------------------------------------- objects

    private static object ReadObject(List<Element> members, Type type, object? existing, int depth, PromptuariumSerializerOptions options)
    {
        if (type == typeof(object))
        {
            type = typeof(Dictionary<string, object?>);
        }

        if (IsScalar(type) || (typeof(IEnumerable).IsAssignableFrom(type) && DictionaryType(type) is null))
        {
            throw new InvalidDataException($"An OBJECT does not fit a {type.Name}.");
        }

        foreach (Element member in members)
        {
            RequireTag(member, CfgFormat.Name, "an object member");
        }

        if (DictionaryType(type) is var (dictionaryType, valueType))
        {
            var dictionary = (IDictionary)(existing ?? Create(dictionaryType));

            foreach (Element member in members)
            {
                string name = DecodeUtf8(member.Data);
                dictionary[name] = ReadHolder(member, valueType, dictionary.Contains(name) ? dictionary[name] : null, depth + 1, options);
            }

            return dictionary;
        }

        TypeShape shape = TypeShape.Of(type, options);

        if (existing is null && !shape.HasDefaultConstructor)
        {
            return Construct(members, type, shape, depth, options);
        }

        // A struct is changed in its boxed copy, which is the result
        object target = existing ?? Create(type);

        foreach (Element member in members)
        {
            if (!shape.ByName.TryGetValue(DecodeUtf8(member.Data), out ShapeMember? property))
            {
                ReadHolder(member, typeof(object), null, depth + 1, options); // validated, then ignored
                continue;
            }

            Assign(target, property, member, depth, options);
        }

        return target;
    }

    // A type without a parameterless constructor (e.g. a positional record): the constructor gets the members that are
    // its parameters, the settable properties get the rest
    private static object Construct(List<Element> members, Type type, TypeShape shape, int depth, PromptuariumSerializerOptions options)
    {
        ConstructorInfo constructor = shape.BindingConstructor
            ?? throw new InvalidDataException($"A {type.Name} has neither a parameterless constructor nor one whose parameters are its members.");

        // The last one of several members with the same name wins
        var byName = new Dictionary<string, Element>(options.NameComparer);

        foreach (Element member in members)
        {
            string name = DecodeUtf8(member.Data);

            if (shape.ByName.TryGetValue(name, out ShapeMember? property))
            {
                byName[property.Name] = member;
            }
            else
            {
                ReadHolder(member, typeof(object), null, depth + 1, options); // validated, then ignored
            }
        }

        ParameterInfo[] parameters = constructor.GetParameters();
        object?[] arguments = new object?[parameters.Length];
        var consumed = new HashSet<string>(options.NameComparer);

        for (int i = 0; i < parameters.Length; i++)
        {
            ParameterInfo parameter = parameters[i];
            ShapeMember property = shape.ByName[parameter.Name!];
            consumed.Add(property.Name);

            object? argument = byName.TryGetValue(property.Name, out Element? member)
                ? ReadMember(member, property, parameter.ParameterType, null, depth, options)
                : null;

            arguments[i] = argument ?? (parameter.HasDefaultValue ? parameter.DefaultValue : DefaultOf(parameter.ParameterType));
        }

        object target = Invoke(() => constructor.Invoke(arguments), type);

        foreach ((string name, Element member) in byName)
        {
            if (!consumed.Contains(name))
            {
                Assign(target, shape.ByName[name], member, depth, options);
            }
        }

        return target;
    }

    // A get-only member is filled in place when it is an object or a dictionary; a value it can not take is ignored,
    // and so is a NULL for a member that can not be null
    private static void Assign(object target, ShapeMember property, Element member, int depth, PromptuariumSerializerOptions options)
    {
        object? current = property.Property.GetValue(target);
        object? value = ReadMember(member, property, property.Property.PropertyType, current, depth, options);

        if (property.CanSet && (value is not null || AcceptsNull(property.Property.PropertyType)))
        {
            Invoke(() => property.Property.SetValue(target, value), target.GetType());
        }
    }

    // A member of an object: its value, of the type declared with PromptuariumTypeAttribute if there is one
    private static object? ReadMember(Element member, ShapeMember property, Type type, object? existing, int depth, PromptuariumSerializerOptions options)
    {
        object? value = ReadHolder(member, type, existing, depth + 1, options);
        byte stored = member.Children[0].Data.Span[0];

        if (property.DeclaredType is { } declared && stored != CfgFormat.Null && stored != declared)
        {
            throw new InvalidDataException($"The member \"{property.Name}\" is declared as {(PromptuariumType)declared}, the file has {(PromptuariumType)stored}.");
        }

        return value;
    }

    // ---------------------------------------------------------------- arrays

    private static object ReadArray(List<Element> items, Type type, int depth, PromptuariumSerializerOptions options)
    {
        if (IsScalar(type) || DictionaryType(type) is not null || (type != typeof(object) && !typeof(IEnumerable).IsAssignableFrom(type)))
        {
            throw new InvalidDataException($"An ARRAY does not fit a {type.Name}.");
        }

        Type elementType = type == typeof(object) ? typeof(object) : ElementType(type);
        var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType))!;

        foreach (Element item in items)
        {
            RequireTag(item, CfgFormat.Item, "an array element");

            if (!item.Data.IsEmpty)
            {
                throw new InvalidDataException("An ITEM node has no data.");
            }

            object? element = ReadHolder(item, elementType, null, depth + 1, options);

            if (element is null && !AcceptsNull(elementType))
            {
                throw new InvalidDataException($"A NULL element does not fit a {elementType.Name}.");
            }

            list.Add(element);
        }

        if (type.IsArray)
        {
            var array = Array.CreateInstance(elementType, list.Count);
            list.CopyTo(array, 0);
            return array;
        }

        if (type.IsAssignableFrom(list.GetType()))
        {
            return list;
        }

        // A collection type of its own (e.g. a subclass of List<T>, a HashSet<T>): filled with its Add method
        object collection = Create(type);
        MethodInfo add = type.GetMethod("Add", [elementType])
            ?? throw new InvalidDataException($"A {type.Name} has no Add({elementType.Name}) method.");

        foreach (object? element in list)
        {
            Invoke(() => add.Invoke(collection, [element]), type);
        }

        return collection;
    }

    // ---------------------------------------------------------------- types

    // The concrete type to create and the value type of a string-keyed dictionary type, or null if it is not one
    private static (Type Concrete, Type Value)? DictionaryType(Type type)
    {
        Type? generic = GenericInterface(type, typeof(IDictionary<,>)) ?? GenericInterface(type, typeof(IReadOnlyDictionary<,>));

        if (generic is null)
        {
            // A non-generic one (e.g. a Hashtable): any values
            return typeof(IDictionary).IsAssignableFrom(type) && !type.IsAbstract ? (type, typeof(object)) : null;
        }

        Type[] arguments = generic.GetGenericArguments();

        if (arguments[0] != typeof(string))
        {
            throw new InvalidDataException($"A {type.Name} is not keyed by strings.");
        }

        if (!type.IsAbstract && typeof(IDictionary).IsAssignableFrom(type))
        {
            return (type, arguments[1]);
        }

        Type concrete = typeof(Dictionary<,>).MakeGenericType(arguments);
        return type.IsAssignableFrom(concrete) ? (concrete, arguments[1]) : throw new InvalidDataException($"A {type.Name} can not be created.");
    }

    private static Type ElementType(Type type)
    {
        if (type.IsArray)
        {
            return type.GetElementType()!;
        }

        return GenericInterface(type, typeof(IEnumerable<>))?.GetGenericArguments()[0]
            ?? throw new InvalidDataException($"An ARRAY does not fit a {type.Name}.");
    }

    private static Type? GenericInterface(Type type, Type definition)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == definition
            ? type
            : Array.Find(type.GetInterfaces(), candidate => candidate.IsGenericType && candidate.GetGenericTypeDefinition() == definition);
    }

    private static bool IsScalar(Type type)
    {
        return type.IsPrimitive || type == typeof(string) || type == typeof(decimal) || type == typeof(byte[]) || ScalarText.Handles(type);
    }

    private static bool AcceptsNull(Type type) => !type.IsValueType || Nullable.GetUnderlyingType(type) is not null;

    private static object? DefaultOf(Type type) => type.IsValueType ? Activator.CreateInstance(type) : null;

    private static object Create(Type type)
    {
        try
        {
            return Activator.CreateInstance(type, nonPublic: true)
                ?? throw new InvalidDataException($"A {type.Name} can not be created.");
        }
        catch (Exception exception) when (exception is MissingMethodException or MemberAccessException or NotSupportedException or ArgumentException or TargetInvocationException)
        {
            throw new InvalidDataException($"A {type.Name} can not be created.", exception);
        }
    }

    // A setter, a constructor or an Add method that rejects the value: the file does not fit the type
    private static T Invoke<T>(Func<T> action, Type type)
    {
        try
        {
            return action();
        }
        catch (TargetInvocationException exception)
        {
            throw new InvalidDataException($"A {type.Name} rejected a value of the file.", exception.InnerException ?? exception);
        }
    }

    private static void Invoke(Action action, Type type) => Invoke(() => { action(); return true; }, type);

    // ---------------------------------------------------------------- scalars

    private static object Fit(object value, Type target)
    {
        return target.IsInstanceOfType(value) ? value : throw new InvalidDataException($"A {value.GetType().Name} does not fit a {target.Name}.");
    }

    private static object FitNumber(double number, Type target)
    {
        if (target == typeof(double) || target == typeof(object))
        {
            return number;
        }

        if (target.IsEnum)
        {
            return Enum.ToObject(target, (long)FitNumber(number, typeof(long)));
        }

        bool integral = target == typeof(sbyte) || target == typeof(byte) || target == typeof(short) || target == typeof(ushort)
            || target == typeof(int) || target == typeof(uint) || target == typeof(long) || target == typeof(ulong);

        if (integral && Math.Floor(number) != number)
        {
            throw new InvalidDataException($"{number.ToString(CultureInfo.InvariantCulture)} does not fit a {target.Name}.");
        }

        if (!integral && target != typeof(float) && target != typeof(decimal))
        {
            throw new InvalidDataException($"A number does not fit a {target.Name}.");
        }

        try
        {
            return Convert.ChangeType(number, target, CultureInfo.InvariantCulture);
        }
        catch (OverflowException exception)
        {
            throw new InvalidDataException($"{number.ToString(CultureInfo.InvariantCulture)} does not fit a {target.Name}.", exception);
        }
    }

    private static object FitText(string text, Type target)
    {
        if (target == typeof(string) || target == typeof(object))
        {
            return text;
        }

        if (ScalarText.Handles(target))
        {
            return ScalarText.TryParse(text, target, out object? value) ? value : throw new InvalidDataException($"\"{text}\" is not a {target.Name}.");
        }

        throw new InvalidDataException($"A string does not fit a {target.Name}.");
    }

    // ---------------------------------------------------------------- nodes

    private static ReadOnlyMemory<byte> ScalarValue(Element holder)
    {
        RequireChildCount(holder, 2);
        Element valueNode = holder.Children[1];
        RequireTag(valueNode, CfgFormat.Value, "the value of a holder");
        RequireLeaf(valueNode);
        return valueNode.Data;
    }

    private static string DecodeUtf8(ReadOnlyMemory<byte> bytes)
    {
        try
        {
            return StrictUtf8.GetString(bytes.Span);
        }
        catch (DecoderFallbackException exception)
        {
            throw new InvalidDataException("A string is not valid UTF-8.", exception);
        }
    }

    private static void RequireTag(Element node, byte tag, string role)
    {
        if (node.MetaData.Length != 1 || node.MetaData.Span[0] != tag)
        {
            throw new InvalidDataException($"Unexpected metadata on {role}.");
        }
    }

    private static void RequireLeaf(Element node)
    {
        if (node.Children.Count != 0)
        {
            throw new InvalidDataException("A TYPE or VALUE node has no children.");
        }
    }

    private static void RequireChildCount(Element holder, int count)
    {
        if (holder.Children.Count != count)
        {
            throw new InvalidDataException("A holder has an unexpected number of children.");
        }
    }
}
