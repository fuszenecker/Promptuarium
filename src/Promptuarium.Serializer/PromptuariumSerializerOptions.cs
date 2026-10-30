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

    internal byte[] SignatureBytes => Encoding.ASCII.GetBytes(_signature);

    internal StringComparer NameComparer => PropertyNameCaseInsensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
}
