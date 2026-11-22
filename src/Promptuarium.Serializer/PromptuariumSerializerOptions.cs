using System.Text;

namespace Promptuarium.Serializer;

/// <summary>
/// Options of <see cref="PromptuariumSerializer"/>. The defaults read and write general objects (<c>PRMT:GEN1-LE</c> or
/// <c>PRMT:GEN2-LE</c>), the layout of <c>SERIALIZER_FORMAT.md</c>; configuration files have
/// <see cref="ConfigurationSignature"/>, and an application can give its files a signature of its own, too. The layout
/// stays the same.
/// </summary>
public sealed class PromptuariumSerializerOptions
{
    /// <summary>
    /// The default signature, <c>PRMT:GEN1-LE</c>: general objects. Written, the version follows the content: a tree with
    /// a type of version 2 (or a NaN or an infinity) is signed <c>PRMT:GEN2-LE</c>; read, both are accepted.
    /// </summary>
    public const string DefaultSignature = "PRMT:GEN1-LE";

    /// <summary>
    /// The signature of configuration files, <c>PRMT:CFG1-LE</c> (<c>SERIALIZER_FORMAT.md</c>). As with
    /// <see cref="DefaultSignature"/>, the version follows the content (<c>PRMT:CFG2-LE</c>), and both are accepted.
    /// </summary>
    public const string ConfigurationSignature = "PRMT:CFG1-LE";

    // The signatures of the layout by kind and version (SERIALIZER_FORMAT.md, 2)
    private static readonly string[][] Kinds =
    [
        ["PRMT:GEN1-LE", "PRMT:GEN2-LE"],
        ["PRMT:CFG1-LE", "PRMT:CFG2-LE"],
    ];

    /// <summary>The options used when none are given.</summary>
    public static PromptuariumSerializerOptions Default { get; } = new();

    private readonly string _signature = DefaultSignature;
    private readonly int _maxDepth = 64;

    /// <summary>
    /// The metadata of the root (BINARY_FORMAT.md, 10): 1 to 15 ASCII bytes, e.g. <c>PRMT:MYAPP1-LE</c>. A reader
    /// rejects every other signature, unless <see cref="UnexpectedSignature"/> decides otherwise. A signature of the
    /// layout (<c>PRMT:GEN1-LE</c>, <c>PRMT:GEN2-LE</c>, <c>PRMT:CFG1-LE</c>, <c>PRMT:CFG2-LE</c>) names its kind: the
    /// version is chosen by the content when writing, and both versions are accepted when reading. A signature of an
    /// application is written and required as it is, and its files may hold the types of version 2.
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

    private readonly Dictionary<Type, ValueConversion> _conversions = [];
    private bool _defaultTypes = true;
    private volatile bool _used;

    /// <summary>
    /// Removes the types stored as text by default: <see cref="Version"/> (e.g. <c>"8.2.2"</c>), <see cref="Uri"/> (its
    /// original string, absolute or relative), <see cref="System.Net.IPAddress"/> (IPv4 and IPv6, e.g.
    /// <c>"192.168.0.1"</c>, <c>"2001:db8::1"</c>) and <see cref="System.Net.IPNetwork"/> (e.g. <c>"10.0.0.0/8"</c>).
    /// Without them, such a value is written as an object of its properties (which does not come back), unless it is
    /// registered with <see cref="AddTextType{T}"/> or <see cref="AddBlobType{T}"/>. A registration wins over a default
    /// type anyway, so this is needed only to have none of them.
    /// </summary>
    /// <returns>These options, for chaining.</returns>
    /// <exception cref="InvalidOperationException">The options are <see cref="Default"/>, or have already been used.</exception>
    public PromptuariumSerializerOptions ClearDefaultTypes()
    {
        ThrowIfReadOnly();
        _defaultTypes = false;
        return this;
    }

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
    /// <see cref="Guid"/>, <c>byte[]</c>, <see cref="object"/>) can not be registered, and a type can be registered
    /// once (as text or as bytes). The options have to be set up before they are used: neither <see cref="Default"/>
    /// nor options already used can be changed.
    /// </remarks>
    /// <returns>These options, for chaining.</returns>
    /// <exception cref="ArgumentException">A type that is stored anyway, or a type registered already.</exception>
    /// <exception cref="InvalidOperationException">The options are <see cref="Default"/>, or have already been used.</exception>
    public PromptuariumSerializerOptions AddTextType<T>(Func<T, string> format, Func<string, T> parse)
    {
        ArgumentNullException.ThrowIfNull(format);
        ArgumentNullException.ThrowIfNull(parse);

        return Add(typeof(T), ValueConversion.Text(format, parse));
    }

    /// <summary>
    /// Stores every value of <typeparamref name="T"/> as BLOB, by the bytes that <paramref name="toBytes"/> gives, and
    /// reads it back with <paramref name="fromBytes"/>: as a member, and in collections and dictionaries too (such a
    /// value can not be the root). It is for the values whose bytes are shorter than their text; it wins over a
    /// <c>[PromptuariumType(Utf8String)]</c> on the type, and serves a property declared as Blob.
    /// </summary>
    /// <remarks>
    /// A BLOB is raw bytes: a reader without the type (e.g. into <see cref="object"/>, pcfg, PStudio) sees bytes, not
    /// the value, so text (<see cref="AddTextType{T}"/>) is the better choice unless the size matters. The rules are
    /// the ones of <see cref="AddTextType{T}"/>: the bytes must give back an equal value when written, bytes that
    /// <paramref name="fromBytes"/> does not take are rejected when read.
    /// </remarks>
    /// <returns>These options, for chaining.</returns>
    /// <exception cref="ArgumentException">A type that is stored anyway, or a type registered already.</exception>
    /// <exception cref="InvalidOperationException">The options are <see cref="Default"/>, or have already been used.</exception>
    public PromptuariumSerializerOptions AddBlobType<T>(Func<T, byte[]> toBytes, Func<byte[], T> fromBytes)
    {
        ArgumentNullException.ThrowIfNull(toBytes);
        ArgumentNullException.ThrowIfNull(fromBytes);

        return Add(typeof(T), ValueConversion.Blob(toBytes, fromBytes));
    }

    private void ThrowIfReadOnly()
    {
        if (ReferenceEquals(this, Default) || _used)
        {
            throw new InvalidOperationException("The options can not be changed: they are the default options, or they have already been used.");
        }
    }

    private PromptuariumSerializerOptions Add(Type registered, ValueConversion conversion)
    {
        ThrowIfReadOnly();

        Type type = Nullable.GetUnderlyingType(registered) ?? registered;

        if (ValueConversion.IsStoredAnyway(type))
        {
            throw new ArgumentException($"A {type.Name} is stored anyway, it can not be registered.", "T");
        }

        if (!_conversions.TryAdd(type, conversion))
        {
            throw new ArgumentException($"{type.Name} is registered already.", "T");
        }

        return this;
    }

    /// <summary>
    /// The conversion of a type stored by a conversion everywhere: registered with <see cref="AddTextType{T}"/> or
    /// <see cref="AddBlobType{T}"/>, a default type (unless <see cref="ClearDefaultTypes"/>), or declared as Utf8String
    /// on the type itself. Null if it is none of them. The options are not changed after this.
    /// </summary>
    internal ValueConversion? ConversionForType(Type type)
    {
        _used = true;

        if (_conversions.Count > 0 && _conversions.TryGetValue(type, out ValueConversion? registered))
        {
            return registered;
        }

        if (_defaultTypes && ValueConversion.Defaults.TryGetValue(type, out ValueConversion? builtIn))
        {
            return builtIn;
        }

        return ValueConversion.ForType(type);
    }

    /// <summary>
    /// The conversion of a property declared as Utf8String or Blob (<see cref="ShapeMember.DeclaredConversion"/>): the
    /// one of its type if it stores as declared, else through <see cref="IParsable{TSelf}"/> (Utf8String); a Blob needs
    /// a registration with <see cref="AddBlobType{T}"/>.
    /// </summary>
    internal ValueConversion ConversionForProperty(ShapeMember member)
    {
        Type type = member.ValueType;

        if (ConversionForType(type) is { } conversion && conversion.StoredType == member.DeclaredType)
        {
            return conversion;
        }

        if (member.DeclaredType == CfgFormat.Utf8String)
        {
            return ValueConversion.OfParsable(type)
                ?? throw new NotSupportedException($"The property {member.Property.Name} is declared as Utf8String, but {type.Name} does not implement IParsable<{type.Name}>, and it is not registered with AddTextType.");
        }

        throw new NotSupportedException($"The property {member.Property.Name} is declared as Blob, but a {type.Name} has no bytes: it is not registered with AddBlobType.");
    }

    /// <summary>The signature written: of a kind of the layout, the version that holds the tree; of an application, itself.</summary>
    internal string SignatureFor(bool needsVersion2)
    {
        return Array.Find(Kinds, kind => kind.Contains(_signature)) is { } versions ? versions[needsVersion2 ? 1 : 0] : _signature;
    }

    /// <summary>Whether a reader accepts <paramref name="metadata"/>: the signature, or (of a kind of the layout) either version of it.</summary>
    internal bool Accepts(ReadOnlySpan<byte> metadata)
    {
        string signature = Encoding.Latin1.GetString(metadata);
        return signature == _signature || (Array.Find(Kinds, kind => kind.Contains(_signature)) is { } versions && versions.Contains(signature));
    }

    /// <summary>Whether <paramref name="metadata"/> is a signature of version 1 of the layout, whose files hold only the types of version 1.</summary>
    internal static bool IsVersion1Signature(ReadOnlySpan<byte> metadata)
    {
        string signature = Encoding.Latin1.GetString(metadata);
        return Array.Exists(Kinds, kind => kind[0] == signature);
    }

    internal StringComparer NameComparer => PropertyNameCaseInsensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
}
