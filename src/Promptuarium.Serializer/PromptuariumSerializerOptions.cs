using System.Text;

namespace Promptuarium.Serializer;

/// <summary>
/// Options of <see cref="PromptuariumSerializer"/>. The defaults read and write <c>PRMT:GEN1-LE</c> files, the layout of
/// <c>SERIALIZER_FORMAT.md</c> with a signature of their own; configuration files have <see cref="ConfigurationSignature"/>,
/// and an application can give its files a signature of its own, too. The layout stays the same.
/// </summary>
public sealed class PromptuariumSerializerOptions
{
    /// <summary>The default signature, <c>PRMT:GEN1-LE</c>: general objects.</summary>
    public const string DefaultSignature = "PRMT:GEN1-LE";

    /// <summary>The signature of configuration files, <c>PRMT:CFG1-LE</c> (<c>SERIALIZER_FORMAT.md</c>).</summary>
    public const string ConfigurationSignature = "PRMT:CFG1-LE";

    /// <summary>The options used when none are given.</summary>
    public static PromptuariumSerializerOptions Default { get; } = new();

    private readonly string _signature = DefaultSignature;
    private readonly int _maxDepth = 64;

    /// <summary>
    /// The metadata of the root (BINARY_FORMAT.md, 10): 1 to 15 ASCII bytes, e.g. <c>PRMT:MYAPP1-LE</c>. A reader
    /// rejects every other signature, unless <see cref="UnexpectedSignature"/> decides otherwise.
    /// </summary>
    public string Signature
    {
        get => _signature;
        init
        {
            ArgumentNullException.ThrowIfNull(value);

            if (value.Length is < 1 or > ElementBuilder.MaxSignatureLength || !Ascii.IsValid(value))
            {
                throw new ArgumentException($"A signature is 1 to {ElementBuilder.MaxSignatureLength} ASCII characters.", nameof(value));
            }

            _signature = value;
        }
    }

    /// <summary>
    /// Decides what a reader does with a file that is not signed with <see cref="Signature"/> (another signature, or a
    /// root with data): it gets the metadata of the root as a string (byte by byte, Latin-1: the same as ASCII for a
    /// signature, and nothing is lost of other bytes), and answers <see cref="SignatureAction.Reject"/>,
    /// <see cref="SignatureAction.Accept"/> or <see cref="SignatureAction.Skip"/>; it can also throw an exception of its
    /// own. Null (the default) rejects every such file.
    /// </summary>
    public Func<string, SignatureAction>? UnexpectedSignature { get; init; }

    /// <summary>Whether member names match regardless of case when reading (default: true, as in IConfiguration).</summary>
    public bool PropertyNameCaseInsensitive { get; init; } = true;

    /// <summary>The deepest nesting written or read (default: 64). It also stops cycles when writing.</summary>
    public int MaxDepth
    {
        get => _maxDepth;
        init => _maxDepth = value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(value), "The depth must be positive.");
    }

    /// <summary>
    /// The limits of loading (default: 16 MiB and 1 000 000 nodes), so that a corrupt or hostile file can not allocate
    /// without bound. Null means no limit.
    /// </summary>
    public LoadOptions? LoadOptions { get; init; } = new() { MaxBytes = 16 * 1024 * 1024, MaxNodes = 1_000_000 };

    private readonly Dictionary<Type, TextConversion> _textTypes = [];
    private volatile bool _used;

    /// <summary>
    /// Stores every value of <typeparamref name="T"/> as UTF8_STRING, by the text that <paramref name="format"/> gives,
    /// and reads it back with <paramref name="parse"/>: as a member, and in collections and dictionaries too (such a
    /// value can not be the root). It is for the types that implement no <see cref="IParsable{TSelf}"/> (e.g.
    /// <see cref="Version"/>, <see cref="Uri"/>), or that are not the application's own, so no attribute can be put on
    /// them; it wins over a <c>[PromptuariumType(Utf8String)]</c> on the type.
    /// </summary>
    /// <remarks>
    /// Writing requires the text to give back an equal value (<see cref="object.Equals(object?)"/>), or the value is
    /// rejected (<see cref="NotSupportedException"/>), so a lossy text is never stored. Reading rejects a text that
    /// <paramref name="parse"/> does not take (a <see cref="FormatException"/>, <see cref="ArgumentException"/> or
    /// <see cref="OverflowException"/>, or null) with <see cref="InvalidDataException"/>. The types that are stored
    /// anyway (numbers, <see cref="bool"/>, strings, chars, enums, the dates, <see cref="TimeSpan"/>,
    /// <see cref="Guid"/>, <c>byte[]</c>, <see cref="object"/>) can not be registered. The options have to be set up
    /// before they are used: neither <see cref="Default"/> nor options already used can be changed.
    /// </remarks>
    /// <returns>These options, for chaining.</returns>
    /// <exception cref="ArgumentException">A type that is stored anyway, or a type registered already.</exception>
    /// <exception cref="InvalidOperationException">The options are <see cref="Default"/>, or have already been used.</exception>
    public PromptuariumSerializerOptions AddTextType<T>(Func<T, string> format, Func<string, T> parse)
    {
        ArgumentNullException.ThrowIfNull(format);
        ArgumentNullException.ThrowIfNull(parse);

        if (ReferenceEquals(this, Default) || _used)
        {
            throw new InvalidOperationException("The options can not be changed: they are the default options, or they have already been used.");
        }

        Type type = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

        if (type.IsPrimitive || type == typeof(decimal) || type == typeof(byte[]) || TextConversion.IsTextOfItsOwn(type))
        {
            throw new ArgumentException($"A {type.Name} is stored anyway, it can not be registered as text.", nameof(T));
        }

        if (!_textTypes.TryAdd(type, TextConversion.Create(format, parse)))
        {
            throw new ArgumentException($"{type.Name} is registered already.", nameof(T));
        }

        return this;
    }

    /// <summary>
    /// The conversion of a type stored as text everywhere: registered with <see cref="AddTextType{T}"/>, or declared as
    /// Utf8String on the type itself. Null if it is neither. The options are not changed after this.
    /// </summary>
    internal TextConversion? TextForType(Type type)
    {
        _used = true;
        return _textTypes.Count > 0 && _textTypes.TryGetValue(type, out TextConversion? registered) ? registered : TextConversion.ForType(type);
    }

    /// <summary>The conversion of a property declared as Utf8String: as <see cref="TextForType"/>, or through <see cref="IParsable{TSelf}"/>.</summary>
    internal TextConversion TextForProperty(ShapeMember member)
    {
        Type type = member.ValueType;

        return TextForType(type) ?? TextConversion.OfParsable(type)
            ?? throw new NotSupportedException($"The property {member.Property.Name} is declared as Utf8String, but {type.Name} does not implement IParsable<{type.Name}>, and it is not registered with AddTextType.");
    }

    internal byte[] SignatureBytes => Encoding.ASCII.GetBytes(_signature);

    internal StringComparer NameComparer => PropertyNameCaseInsensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
}
