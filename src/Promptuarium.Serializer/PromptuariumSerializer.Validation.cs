using System.Globalization;
using System.Text;

namespace Promptuarium.Serializer;

/// <summary>
/// A problem of a tree found by <see cref="PromptuariumSerializer.Validate"/>: an error (the tree does not follow
/// SERIALIZER_FORMAT.md, it can not be read) or a warning (it can be read, but e.g. a repeated name hides the earlier
/// members).
/// </summary>
/// <param name="IsError">True for an error, false for a warning.</param>
/// <param name="Path">The path of the node the problem is about, as pedit writes it: "/" for the root, "/0/2" for the third child of the first child.</param>
/// <param name="Message">What is wrong.</param>
public sealed record ValidationProblem(bool IsError, string Path, string Message)
{
    /// <inheritdoc/>
    public override string ToString() => $"{(IsError ? "error" : "warning")}: {Path}: {Message}";
}

public static partial class PromptuariumSerializer
{
    /// <summary>
    /// Checks the structure of a tree by the rules of SERIALIZER_FORMAT.md: a root without data, the tags, the type codes,
    /// the children of the holders, the values (their sizes and forms, the nested trees) and the comments, within the
    /// <see cref="PromptuariumSerializerOptions.MaxDepth"/> and the <see cref="PromptuariumSerializerOptions.LoadOptions"/>
    /// of <paramref name="options"/>. The signature (the metadata of the root) is not checked: a tree without errors can
    /// be read with any signature that the reader accepts. Every problem is reported, not only the first one; the types
    /// of version 2 in a tree signed with a signature of version 1, and repeated names, are warnings.
    /// </summary>
    public static IReadOnlyList<ValidationProblem> Validate(Element root, PromptuariumSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(root);
        options ??= PromptuariumSerializerOptions.Default;

        var validation = new Validation(options, PromptuariumSerializerOptions.IsVersion1Signature(root.MetaData.Span) ? Encoding.Latin1.GetString(root.MetaData.Span) : null);

        if (!root.Data.IsEmpty)
        {
            validation.Error("/", "the root has data");
        }

        validation.CheckContent(root, "/", CfgFormat.Object, first: 0, depth: 1);
        return validation.Problems;
    }

    /// <summary>
    /// Whether a tree follows SERIALIZER_FORMAT.md: <see cref="Validate"/> finds no error in it (warnings are allowed).
    /// The signature is not checked.
    /// </summary>
    public static bool IsValid(Element root, PromptuariumSerializerOptions? options = null) => !Validate(root, options).Any(problem => problem.IsError);

    private sealed class Validation(PromptuariumSerializerOptions options, string? version1Signature)
    {
        public List<ValidationProblem> Problems { get; } = [];

        public void Error(string path, string message) => Problems.Add(new ValidationProblem(true, path, message));

        public void Warning(string path, string message) => Problems.Add(new ValidationProblem(false, path, message));

        // The children of a holder (or of the root, an implicit OBJECT) from first, by the type of the holder
        public void CheckContent(Element holder, string path, byte type, int first, int depth)
        {
            int values = 0;
            var names = new Dictionary<string, int>(options.NameComparer);

            for (int i = first; i < holder.Children.Count; i++)
            {
                Element child = holder.Children[i];
                string childPath = ChildPath(path, i);

                if (child.MetaData.Length != 1 || child.MetaData.Span[0] > CfgFormat.Comment)
                {
                    string metadata = child.MetaData.IsEmpty ? "no metadata" : $"the metadata {Convert.ToHexString(child.MetaData.Span)}";
                    Error(childPath, $"the node has {metadata}, not a tag");
                    continue;
                }

                byte tag = child.MetaData.Span[0];

                switch (tag)
                {
                    case CfgFormat.Comment:
                        CheckLeaf(child, childPath, "COMMENT");
                        CheckUtf8(child.Data.Span, childPath, "the comment");
                        break;

                    case CfgFormat.Name when type == CfgFormat.Object:
                        if (CheckUtf8(child.Data.Span, childPath, "the name"))
                        {
                            string name = StrictUtf8.GetString(child.Data.Span);
                            names[name] = names.GetValueOrDefault(name) + 1;
                        }

                        CheckHolder(child, childPath, depth + 1);
                        break;

                    case CfgFormat.Item when type == CfgFormat.Array:
                        if (!child.Data.IsEmpty)
                        {
                            Error(childPath, "the ITEM node has data");
                        }

                        CheckHolder(child, childPath, depth + 1);
                        break;

                    case CfgFormat.Value when IsScalarType(type):
                        values++;
                        CheckLeaf(child, childPath, "VALUE");

                        if (values == 1)
                        {
                            CheckValue(child.Data, childPath, type, depth);
                        }

                        break;

                    default:
                        string where = holder.Parent is null && first == 0 ? "the root" : $"a holder of {TypeName(type)}";
                        Error(childPath, $"a {TagName(tag)} node can not be in {where}");
                        break;
                }
            }

            if (IsScalarType(type) && values != 1)
            {
                Error(path, string.Create(CultureInfo.InvariantCulture, $"a holder of {TypeName(type)} has {values} VALUE nodes, not one"));
            }

            foreach ((string name, int count) in names.Where(pair => pair.Value > 1))
            {
                Warning(path, string.Create(CultureInfo.InvariantCulture, $"the name \"{name}\" is repeated {count} times; the last one wins"));
            }
        }

        private void CheckHolder(Element holder, string path, int depth)
        {
            if (depth > options.MaxDepth)
            {
                Error(path, string.Create(CultureInfo.InvariantCulture, $"nested deeper than {options.MaxDepth} levels"));
                return;
            }

            if (holder.Children.Count == 0 || holder.Children[0].MetaData.Length != 1 || holder.Children[0].MetaData.Span[0] != CfgFormat.Type)
            {
                Error(path, "the first child of the holder is not a TYPE node");
                return;
            }

            Element typeNode = holder.Children[0];
            string typePath = ChildPath(path, 0);
            CheckLeaf(typeNode, typePath, "TYPE");

            if (typeNode.Data.Length != 1)
            {
                Error(typePath, string.Create(CultureInfo.InvariantCulture, $"the TYPE node has {typeNode.Data.Length} bytes, not one type code"));
                return;
            }

            byte type = typeNode.Data.Span[0];

            if (!CfgFormat.IsSupported(type))
            {
                Error(typePath, $"the type code 0x{type:X2} is {(type is 0x60 or 0x64 ? "reserved" : "unknown")}");
                return;
            }

            if (version1Signature is not null && !CfgFormat.IsVersion1(type))
            {
                Warning(typePath, $"the type {TypeName(type)} is of version 2, but the tree is signed \"{version1Signature}\" (version 1)");
            }

            CheckContent(holder, path, type, first: 1, depth);
        }

        private void CheckValue(ReadOnlyMemory<byte> value, string path, byte type, int depth)
        {
            try
            {
                switch (type)
                {
                    case CfgFormat.Boolean:
                        if (value.Length != 1 || value.Span[0] > 1)
                        {
                            Error(path, $"the BOOLEAN value {Convert.ToHexString(value.Span)} is not 00 or 01");
                        }

                        break;

                    case CfgFormat.Utf8String:
                        CheckUtf8(value.Span, path, "the UTF8_STRING value");
                        break;

                    case CfgFormat.Blob:
                        break;

                    case CfgFormat.Container:
                        Element tree = Containers.Decode(value, options.LoadOptions);

                        if (Containers.DeepestLevel(tree, depth) > options.MaxDepth)
                        {
                            Error(path, string.Create(CultureInfo.InvariantCulture, $"the nested tree is deeper than {options.MaxDepth} levels with the file"));
                        }

                        break;

                    default:
                        object decoded = Scalars.Decode(type, value, typeof(object));

                        if (version1Signature is not null && type == CfgFormat.Float64 && decoded is double number && !double.IsFinite(number))
                        {
                            Warning(path, $"the FLOAT64 value is {number.ToString(CultureInfo.InvariantCulture)}, which a tree of version 1 (\"{version1Signature}\") can not hold");
                        }

                        break;
                }
            }
            catch (InvalidDataException exception)
            {
                Error(path, $"the {TypeName(type)} value is not valid: {exception.Message}");
            }
        }

        private void CheckLeaf(Element node, string path, string tag)
        {
            if (node.Children.Count > 0)
            {
                Error(path, $"the {tag} node has children");
            }
        }

        private bool CheckUtf8(ReadOnlySpan<byte> bytes, string path, string what)
        {
            try
            {
                StrictUtf8.GetCharCount(bytes);
                return true;
            }
            catch (DecoderFallbackException)
            {
                Error(path, $"{what} is not valid UTF-8");
                return false;
            }
        }

        private static bool IsScalarType(byte type) => type is not (CfgFormat.Null or CfgFormat.Object or CfgFormat.Array);

        private static string ChildPath(string path, int index) => string.Create(CultureInfo.InvariantCulture, $"{(path == "/" ? string.Empty : path)}/{index}");

        private static string TagName(byte tag) => tag switch
        {
            CfgFormat.Name => "NAME",
            CfgFormat.Type => "TYPE",
            CfgFormat.Item => "ITEM",
            CfgFormat.Value => "VALUE",
            _ => "COMMENT",
        };

        // The names of SERIALIZER_FORMAT.md (4.4): INT32, UTF8_STRING, ...
        private static string TypeName(byte type) => (PromptuariumType)type switch
        {
            PromptuariumType.Utf8String => "UTF8_STRING",
            var known when Enum.IsDefined(known) => known.ToString().ToUpperInvariant(),
            _ => $"0x{type:X2}",
        };
    }
}
