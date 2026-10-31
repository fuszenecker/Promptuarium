using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Promptuarium;

namespace PEdit;

/// <summary>
/// Exit codes of the tool.
/// </summary>
internal static class ExitCodes
{
    /// <summary>Success.</summary>
    public const int Success = 0;

    /// <summary>Invalid command line: unknown command or option, missing argument, invalid path or content.</summary>
    public const int Usage = 1;

    /// <summary>The file or the node does not exist.</summary>
    public const int NotFound = 2;

    /// <summary>The operation is not possible: invalid stream, existing file, deleting the root, I/O error.</summary>
    public const int Failed = 3;
}

/// <summary>
/// An error with the exit code of the tool.
/// </summary>
internal sealed class PEditException : Exception
{
    /// <summary>Creates an error.</summary>
    public PEditException(int exitCode, string message)
        : base(message)
    {
        ExitCode = exitCode;
    }

    /// <summary>Creates an error.</summary>
    public PEditException()
    {
    }

    /// <summary>Creates an error.</summary>
    public PEditException(string message)
        : base(message)
    {
    }

    /// <summary>Creates an error.</summary>
    public PEditException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>The exit code.</summary>
    public int ExitCode { get; } = ExitCodes.Failed;
}

/// <summary>
/// The commands of pedit. Every command is a single, non-interactive call; with --json the output is machine-readable.
/// </summary>
internal static class Commands
{
    /// <summary>
    /// The help text.
    /// </summary>
    public const string Usage = """
        pedit - creates, dumps and edits Promptuarium trees from the command line

        Usage:
          pedit new    FILE [--signature VALUE] [--force]
          pedit dump   FILE [--path PATH] [--max-bytes N] [--json]
          pedit chunks FILE [--json]
          pedit add    FILE PARENT-PATH [--index N] [--metadata VALUE] [--data VALUE] [--output FILE] [--json]
          pedit set    FILE PATH [--metadata VALUE] [--data VALUE] [--output FILE] [--json]
          pedit delete FILE PATH [--output FILE] [--json]
          pedit get    FILE PATH metadata|data [--format hex|base64|utf8] [--out FILE]
          pedit help

        Commands:
          new     Creates a file with an empty root (or a signed root, see BINARY_FORMAT.md, section 10).
          dump    Lists the nodes (pre-order), with their paths and contents.
          chunks  Lists the chunks of the file, decoded directly from the specification.
          add     Adds a new node to PARENT-PATH (at the end, or at --index), prints its path.
          set     Sets the metadata and/or the data of a node.
          delete  Deletes a node with its subtree (not the root).
          get     Prints the metadata or the data of a node (or writes the raw bytes to --out).

        Paths:   '/' is the root, '/0/2' is the third child of the first child of the root.
        VALUE:   hex:4152 | utf8:TEXT | ascii:TEXT | base64:QVI= | file:PATH | empty
        Editing commands save the file in place, or into --output FILE.

        Exit codes: 0 success, 1 invalid command line, 2 file or node not found, 3 operation failed.
        """;

    private static readonly HashSet<string> ValueOptions = new() { "--signature", "--path", "--max-bytes", "--index", "--data", "--metadata", "--output", "--format", "--out" };
    private static readonly HashSet<string> FlagOptions = new() { "--json", "--force" };
    private static readonly JsonWriterOptions JsonOptions = new() { Indented = true };

    /// <summary>
    /// Runs a command.
    /// </summary>
    /// <returns>The exit code</returns>
    public static async Task<int> RunAsync(string[] args, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        try
        {
            if (args.Length == 0)
            {
                throw new PEditException(ExitCodes.Usage, "No command given.");
            }

            var command = new Arguments(args.Skip(1));

            switch (args[0])
            {
                case "help":
                case "--help":
                case "-h":
                    output.WriteLine(Usage);
                    return ExitCodes.Success;
                case "new":
                    await NewAsync(command, output).ConfigureAwait(false);
                    break;
                case "dump":
                    await DumpAsync(command, output).ConfigureAwait(false);
                    break;
                case "chunks":
                    return Chunks(command, output, error);
                case "add":
                    await AddAsync(command, output).ConfigureAwait(false);
                    break;
                case "set":
                    await SetAsync(command, output).ConfigureAwait(false);
                    break;
                case "delete":
                    await DeleteAsync(command, output).ConfigureAwait(false);
                    break;
                case "get":
                    await GetAsync(command, output).ConfigureAwait(false);
                    break;
                default:
                    throw new PEditException(ExitCodes.Usage, $"Unknown command '{args[0]}'.");
            }

            return ExitCodes.Success;
        }
        catch (PEditException ex)
        {
            error.WriteLine($"pedit: {ex.Message}");

            if (ex.ExitCode == ExitCodes.Usage)
            {
                error.WriteLine("Run 'pedit help' for the usage.");
            }

            return ex.ExitCode;
        }
    }

    #region Commands

    private static async Task NewAsync(Arguments command, TextWriter output)
    {
        command.Expect(1, "new FILE");
        string file = command.Positional(0);

        if (File.Exists(file) && !command.Flag("--force"))
        {
            throw new PEditException(ExitCodes.Failed, $"{file} already exists (use --force to overwrite it).");
        }

        Element root = new Element();
        string? signature = command.Value("--signature");

        if (signature != null)
        {
            byte[] bytes = ContentValue.Parse(signature) ?? throw new PEditException(ExitCodes.Usage, "The signature can not be empty.");

            try
            {
                root = new ElementBuilder().WithSignature(bytes).Build();
            }
            catch (ArgumentException ex)
            {
                throw new PEditException(ExitCodes.Usage, ex.Message);
            }
        }

        await SaveAsync(root, file).ConfigureAwait(false);
        Report(command, output, "created", NodePath.Root);
    }

    private static async Task DumpAsync(Arguments command, TextWriter output)
    {
        command.Expect(1, "dump FILE");
        Element root = await LoadAsync(command.Positional(0)).ConfigureAwait(false);
        NodePath start = NodePath.Parse(command.Value("--path") ?? "/");
        Element first = start.Resolve(root);
        int? maxBytes = command.IntValue("--max-bytes");
        bool json = command.Flag("--json");

        // Pre-order, without recursion
        var nodes = new Stack<(Element Node, NodePath Path, int Depth)>();
        nodes.Push((first, start, 0));

        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer, JsonOptions))
        {
            if (json)
            {
                writer.WriteStartObject();
                writer.WriteStartArray("nodes");
            }

            while (nodes.Count > 0)
            {
                (Element node, NodePath path, int depth) = nodes.Pop();

                if (json)
                {
                    writer.WriteStartObject();
                    writer.WriteString("path", path.ToString());
                    writer.WriteNumber("depth", depth);
                    writer.WriteNumber("children", node.Children.Count);
                    WriteContent(writer, "metadata", node.MetaData, maxBytes);
                    WriteContent(writer, "data", node.Data, maxBytes);
                    writer.WriteEndObject();
                }
                else
                {
                    output.WriteLine($"{new string(' ', depth * 2)}{path}  {Describe(node, maxBytes ?? 32)}");
                }

                for (int index = node.Children.Count - 1; index >= 0; index--)
                {
                    nodes.Push((node.Children[index], path.Child(index), depth + 1));
                }
            }

            if (json)
            {
                writer.WriteEndArray();
                writer.WriteEndObject();
            }
        }

        if (json)
        {
            output.WriteLine(Encoding.UTF8.GetString(buffer.ToArray()));
        }
    }

    private static int Chunks(Arguments command, TextWriter output, TextWriter error)
    {
        command.Expect(1, "chunks FILE");
        string file = command.Positional(0);
        List<Chunk> chunks;
        long consumed;
        long length;
        string? decodeError;

        try
        {
            using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
            length = stream.Length;
            using var buffered = new BufferedStream(stream);
            (chunks, consumed, decodeError) = ChunkDecoder.Decode(buffered);
        }
        catch (FileNotFoundException)
        {
            throw new PEditException(ExitCodes.NotFound, $"{file} does not exist.");
        }
        catch (DirectoryNotFoundException)
        {
            throw new PEditException(ExitCodes.NotFound, $"{file} does not exist.");
        }

        if (command.Flag("--json"))
        {
            using var buffer = new MemoryStream();
            using (var writer = new Utf8JsonWriter(buffer, JsonOptions))
            {
                writer.WriteStartObject();
                writer.WriteStartArray("chunks");

                foreach (Chunk chunk in chunks)
                {
                    writer.WriteStartObject();
                    writer.WriteNumber("offset", chunk.Offset);
                    writer.WriteString("control", chunk.Control.ToString("X2", CultureInfo.InvariantCulture));
                    writer.WriteString("direction", chunk.Direction);
                    writer.WriteString("type", chunk.ContentType);
                    writer.WriteString("size", chunk.SizeType);
                    writer.WriteNumber("length", chunk.Length);
                    writer.WriteString("kind", chunk.Kind);
                    writer.WriteNumber("depth", chunk.Depth);
                    writer.WriteString("payload", Convert.ToHexString(chunk.Payload.Span));
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
                writer.WriteNumber("consumed", consumed);
                writer.WriteNumber("trailing", length - consumed);

                if (decodeError != null)
                {
                    writer.WriteString("error", decodeError);
                }
                else
                {
                    writer.WriteNull("error");
                }

                writer.WriteEndObject();
            }

            output.WriteLine(Encoding.UTF8.GetString(buffer.ToArray()));
        }
        else
        {
            output.WriteLine("offset  ctl  direction type      size         length  depth  kind        payload");

            foreach (Chunk chunk in chunks)
            {
                output.WriteLine(string.Create(CultureInfo.InvariantCulture,
                    $"{chunk.Offset,6}  {chunk.Control:X2}   {chunk.Direction,-9} {chunk.ContentType,-9} {chunk.SizeType,-12} {chunk.Length,6}  {chunk.Depth,5}  {chunk.Kind,-10}  {Preview(chunk.Payload.ToArray(), 16)}"));
            }

            output.WriteLine(string.Create(CultureInfo.InvariantCulture, $"{consumed} bytes consumed, {length - consumed} trailing bytes"));
        }

        if (decodeError != null)
        {
            error.WriteLine($"pedit: {decodeError}");
            return ExitCodes.Failed;
        }

        return ExitCodes.Success;
    }

    private static async Task AddAsync(Arguments command, TextWriter output)
    {
        command.Expect(2, "add FILE PARENT-PATH");
        string file = command.Positional(0);
        Element root = await LoadAsync(file).ConfigureAwait(false);
        NodePath parentPath = NodePath.Parse(command.Positional(1));
        Element parent = parentPath.Resolve(root);

        var node = new Element(ToContent(command.Value("--data")), ToContent(command.Value("--metadata")));
        int index = command.IntValue("--index") ?? parent.Children.Count;

        if (index > parent.Children.Count)
        {
            throw new PEditException(ExitCodes.Usage, $"Invalid index {index}: {parentPath} has {parent.Children.Count} children.");
        }

        parent.Insert(index, node);

        await SaveAsync(root, command.Value("--output") ?? file).ConfigureAwait(false);
        Report(command, output, "added", parentPath.Child(index));
    }

    private static async Task SetAsync(Arguments command, TextWriter output)
    {
        command.Expect(2, "set FILE PATH");
        string? data = command.Value("--data");
        string? metaData = command.Value("--metadata");

        if (data == null && metaData == null)
        {
            throw new PEditException(ExitCodes.Usage, "Nothing to set: give --metadata and/or --data.");
        }

        string file = command.Positional(0);
        Element root = await LoadAsync(file).ConfigureAwait(false);
        NodePath path = NodePath.Parse(command.Positional(1));
        Element node = path.Resolve(root);

        if (data != null)
        {
            node.Data = ToContent(data);
        }

        if (metaData != null)
        {
            node.MetaData = ToContent(metaData);
        }

        await SaveAsync(root, command.Value("--output") ?? file).ConfigureAwait(false);
        Report(command, output, "set", path);
    }

    private static async Task DeleteAsync(Arguments command, TextWriter output)
    {
        command.Expect(2, "delete FILE PATH");
        string file = command.Positional(0);
        Element root = await LoadAsync(file).ConfigureAwait(false);
        NodePath path = NodePath.Parse(command.Positional(1));

        if (path.IsRoot)
        {
            throw new PEditException(ExitCodes.Failed, "The root can not be deleted (create a new file instead).");
        }

        Element node = path.Resolve(root);
        node.Parent!.Remove(node);

        await SaveAsync(root, command.Value("--output") ?? file).ConfigureAwait(false);
        Report(command, output, "deleted", path);
    }

    private static async Task GetAsync(Arguments command, TextWriter output)
    {
        command.Expect(3, "get FILE PATH metadata|data");
        Element root = await LoadAsync(command.Positional(0)).ConfigureAwait(false);
        Element node = NodePath.Parse(command.Positional(1)).Resolve(root);

        ReadOnlyMemory<byte> content = command.Positional(2) switch
        {
            "data" => node.Data,
            "metadata" => node.MetaData,
            string other => throw new PEditException(ExitCodes.Usage, $"'{other}' is neither 'metadata' nor 'data'."),
        };

        byte[] bytes = content.ToArray();
        string? outFile = command.Value("--out");

        if (outFile != null)
        {
            await File.WriteAllBytesAsync(outFile, bytes).ConfigureAwait(false);
            return;
        }

        string format = command.Value("--format") ?? "hex";

        output.WriteLine(format switch
        {
            "hex" => Convert.ToHexString(bytes),
            "base64" => Convert.ToBase64String(bytes),
            "utf8" => Encoding.UTF8.GetString(bytes),
            _ => throw new PEditException(ExitCodes.Usage, $"Unknown format '{format}' (use hex, base64 or utf8)."),
        });
    }

    #endregion

    #region Helpers

    private static async Task<Element> LoadAsync(string file)
    {
        try
        {
            return await Element.LoadAsync(file).ConfigureAwait(false);
        }
        catch (FileNotFoundException)
        {
            throw new PEditException(ExitCodes.NotFound, $"{file} does not exist.");
        }
        catch (DirectoryNotFoundException)
        {
            throw new PEditException(ExitCodes.NotFound, $"{file} does not exist.");
        }
        catch (PromptuariumException ex)
        {
            throw new PEditException(ExitCodes.Failed, $"{file} is not a valid Promptuarium stream: {ex.Message}");
        }
    }

    /// <summary>
    /// Saves into a temporary file first, then replaces the target, so a failure never leaves a half-written file.
    /// </summary>
    private static async Task SaveAsync(Element root, string file)
    {
        string directory = Path.GetDirectoryName(Path.GetFullPath(file))!;
        string temporary = Path.Combine(directory, $".{Path.GetFileName(file)}.{Guid.NewGuid():N}.tmp");

        try
        {
            await root.SaveAsync(temporary).ConfigureAwait(false);
            File.Move(temporary, file, overwrite: true);
        }
        catch (IOException ex)
        {
            throw new PEditException(ExitCodes.Failed, $"Can not save {file}: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new PEditException(ExitCodes.Failed, $"Can not save {file}: {ex.Message}");
        }
        finally
        {
            File.Delete(temporary);
        }
    }

    private static ReadOnlyMemory<byte> ToContent(string? value)
    {
        return value != null ? ContentValue.Parse(value) : null;
    }

    private static void Report(Arguments command, TextWriter output, string action, NodePath path)
    {
        if (command.Flag("--json"))
        {
            output.WriteLine($$"""{ "{{action}}": "{{path}}" }""");
        }
        else
        {
            output.WriteLine($"{action} {path}");
        }
    }

    private static string Describe(Element node, int maxBytes)
    {
        bool hasData = !node.Data.IsEmpty;
        bool hasMetaData = !node.MetaData.IsEmpty;

        if (!hasData && !hasMetaData)
        {
            return node.Children.Count > 0 ? "(no content)" : "(empty)";
        }

        // The metadata first: it is short, mostly a tag, and tells what the data is
        var parts = new List<string>();

        if (hasMetaData)
        {
            parts.Add(DescribeContent("metadata", node.MetaData.ToArray(), maxBytes));
        }

        if (hasData)
        {
            parts.Add(DescribeContent("data", node.Data.ToArray(), maxBytes));
        }

        return string.Join("  ", parts);
    }

    private static string DescribeContent(string name, byte[] bytes, int maxBytes)
    {
        string text = new(bytes.Take(maxBytes).Select(b => b >= 0x20 && b < 0x7F ? (char)b : '.').ToArray());
        return string.Create(CultureInfo.InvariantCulture, $"{name}({bytes.Length})={Preview(bytes, maxBytes)} \"{text}{(bytes.Length > maxBytes ? "..." : string.Empty)}\"");
    }

    private static string Preview(byte[] bytes, int maxBytes)
    {
        string hex = Convert.ToHexString(bytes, 0, Math.Min(bytes.Length, maxBytes));
        return bytes.Length > maxBytes ? hex + "..." : hex;
    }

    private static void WriteContent(Utf8JsonWriter writer, string name, ReadOnlyMemory<byte> content, int? maxBytes)
    {
        if (content.IsEmpty)
        {
            writer.WriteNull(name);
            return;
        }

        byte[] bytes = content.ToArray();
        byte[] shown = maxBytes < bytes.Length ? bytes[..maxBytes.Value] : bytes;

        writer.WriteStartObject(name);
        writer.WriteNumber("length", bytes.Length);
        writer.WriteString("hex", Convert.ToHexString(shown));
        writer.WriteBoolean("truncated", shown.Length < bytes.Length);

        // The text form only if the content is valid UTF-8
        try
        {
            writer.WriteString("utf8", new UTF8Encoding(false, true).GetString(shown));
        }
        catch (DecoderFallbackException)
        {
            writer.WriteNull("utf8");
        }

        writer.WriteEndObject();
    }

    #endregion

    /// <summary>
    /// The positional arguments and the options of a command.
    /// </summary>
    private sealed class Arguments
    {
        private readonly List<string> positionals = new();
        private readonly Dictionary<string, string> values = new();
        private readonly HashSet<string> flags = new();

        public Arguments(IEnumerable<string> args)
        {
            using IEnumerator<string> enumerator = args.GetEnumerator();

            while (enumerator.MoveNext())
            {
                string arg = enumerator.Current;

                if (ValueOptions.Contains(arg))
                {
                    if (!enumerator.MoveNext())
                    {
                        throw new PEditException(ExitCodes.Usage, $"Option {arg} needs a value.");
                    }

                    values[arg] = enumerator.Current;
                }
                else if (FlagOptions.Contains(arg))
                {
                    flags.Add(arg);
                }
                else if (arg.StartsWith("--", StringComparison.Ordinal))
                {
                    throw new PEditException(ExitCodes.Usage, $"Unknown option {arg}.");
                }
                else
                {
                    positionals.Add(arg);
                }
            }
        }

        public void Expect(int count, string usage)
        {
            if (positionals.Count != count)
            {
                throw new PEditException(ExitCodes.Usage, $"Usage: pedit {usage}");
            }
        }

        public string Positional(int index) => positionals[index];

        public string? Value(string name) => values.GetValueOrDefault(name);

        public bool Flag(string name) => flags.Contains(name);

        public int? IntValue(string name)
        {
            string? value = Value(name);

            if (value == null)
            {
                return null;
            }

            if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int result))
            {
                throw new PEditException(ExitCodes.Usage, $"Option {name} needs a non-negative number, not '{value}'.");
            }

            return result;
        }
    }
}
