using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Promptuarium;

namespace PCfg;

/// <summary>
/// Exit codes of the tool.
/// </summary>
internal static class ExitCodes
{
    /// <summary>Success.</summary>
    public const int Success = 0;

    /// <summary>Invalid command line: unknown command or option, missing argument, invalid key or value.</summary>
    public const int Usage = 1;

    /// <summary>The file or the key does not exist.</summary>
    public const int NotFound = 2;

    /// <summary>The operation failed: invalid file, existing file, a key through a scalar, I/O error.</summary>
    public const int Failed = 3;
}

/// <summary>
/// An error with the exit code of the tool.
/// </summary>
internal sealed class PCfgException : Exception
{
    /// <summary>Creates an error.</summary>
    public PCfgException(int exitCode, string message)
        : base(message)
    {
        ExitCode = exitCode;
    }

    /// <summary>Creates an error.</summary>
    public PCfgException()
    {
    }

    /// <summary>Creates an error.</summary>
    public PCfgException(string message)
        : base(message)
    {
    }

    /// <summary>Creates an error.</summary>
    public PCfgException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>The exit code.</summary>
    public int ExitCode { get; } = ExitCodes.Failed;
}

/// <summary>
/// The commands of pcfg. Every command is a single, non-interactive call; the values are addressed by their keys.
/// </summary>
internal static class Commands
{
    /// <summary>
    /// The help text.
    /// </summary>
    public const string Usage = """
        pcfg - shows and edits Promptuarium configuration files (PRMT:CFG1-LE, SERIALIZER_FORMAT.md)

        Usage:
          pcfg new     FILE [--comment TEXT] [--force]
          pcfg show    FILE [KEY] [--flat] [--max-bytes N]
          pcfg get     FILE KEY [--json] [--out FILE]
          pcfg set     FILE KEY VALUE [--comment TEXT] [--output FILE]
          pcfg delete  FILE KEY [--output FILE]
          pcfg rename  FILE KEY NEW-NAME [--output FILE]
          pcfg comment FILE KEY [TEXT] [--add] [--output FILE]
          pcfg import  FILE JSON-FILE|- [--force]
          pcfg export  FILE [KEY] [--output JSON-FILE]
          pcfg check   FILE [--strict]
          pcfg help

        Commands:
          new      Creates a configuration without members.
          show     Shows the configuration (or the value of KEY) as a tree, or as key = value lines (--flat).
          get      Prints a value: a string as it is, a BLOB as hex, a number, true, false or null.
          set      Sets a value; the missing objects on the way are created. The comments of the value are kept.
          delete   Deletes a value (every member with the name).
          rename   Renames a member of an object.
          comment  Replaces the comments of a value (or of the file, KEY ''); --add appends, no TEXT removes them.
          import   Creates a configuration from a JSON file (e.g. appsettings.json; comments are allowed).
          export   Writes the configuration as JSON (a BLOB as base64, without the comments).
          check    Checks the file by SERIALIZER_FORMAT.md and lists every problem.

        KEY:     names and indexes separated by ':', e.g. Logging:LogLevel:Default or Hosts:0 ('' is the root);
                 a ':' or a '\' in a name is escaped with '\'.
        VALUE:   string:TEXT | number:1.5 | bool:true | null | {} | [] | hex:DEADBEEF | base64:3q2+7w== | file:PATH | json:JSON
                 Without a form, an existing value keeps its type; a new one is true/false, a number, or a string.
        --signature SIG  The signature of the file (default PRMT:CFG1-LE), e.g. PRMT:GEN1-LE. Every command takes it.
        Editing commands save the file in place, or into --output FILE.

        Exit codes: 0 success, 1 invalid command line, 2 file or key not found, 3 operation failed or invalid file.
        """;

    private static readonly HashSet<string> ValueOptions = new() { "--signature", "--max-bytes", "--output", "--out", "--comment" };
    private static readonly HashSet<string> FlagOptions = new() { "--json", "--force", "--flat", "--add", "--strict" };

    /// <summary>
    /// Runs a command.
    /// </summary>
    /// <returns>The exit code</returns>
    public static async Task<int> RunAsync(string[] args, TextReader input, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        try
        {
            if (args.Length == 0)
            {
                throw new PCfgException(ExitCodes.Usage, "No command given.");
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
                case "show":
                    await ShowAsync(command, output).ConfigureAwait(false);
                    break;
                case "get":
                    await GetAsync(command, output).ConfigureAwait(false);
                    break;
                case "set":
                    await SetAsync(command, output).ConfigureAwait(false);
                    break;
                case "delete":
                    await DeleteAsync(command, output).ConfigureAwait(false);
                    break;
                case "rename":
                    await RenameAsync(command, output).ConfigureAwait(false);
                    break;
                case "comment":
                    await CommentAsync(command, output).ConfigureAwait(false);
                    break;
                case "import":
                    await ImportAsync(command, input, output).ConfigureAwait(false);
                    break;
                case "export":
                    await ExportAsync(command, output).ConfigureAwait(false);
                    break;
                case "check":
                    return await CheckAsync(command, output, error).ConfigureAwait(false);
                default:
                    throw new PCfgException(ExitCodes.Usage, $"Unknown command '{args[0]}'.");
            }

            return ExitCodes.Success;
        }
        catch (PCfgException ex)
        {
            error.WriteLine($"pcfg: {ex.Message}");

            if (ex.ExitCode == ExitCodes.Usage)
            {
                error.WriteLine("Run 'pcfg help' for the usage.");
            }

            return ex.ExitCode;
        }
    }

    #region Commands

    private static async Task NewAsync(Arguments command, TextWriter output)
    {
        command.Expect(1, 1, "new FILE");
        string file = command.Positional(0);
        EnsureNew(file, command);

        Element root = NewRoot(command);
        string? comment = command.Value("--comment");

        if (comment != null)
        {
            CfgLayout.SetComments(root, [comment]);
        }

        await SaveAsync(root, file).ConfigureAwait(false);
        output.WriteLine($"created {file}");
    }

    private static async Task ShowAsync(Arguments command, TextWriter output)
    {
        command.Expect(1, 2, "show FILE [KEY]");
        Element root = await LoadAsync(command).ConfigureAwait(false);
        ConfigKey key = command.Count > 1 ? ConfigKey.Parse(command.Positional(1)) : ConfigKey.Root;
        Element holder = Find(root, key);
        int maxBytes = command.IntValue("--max-bytes") ?? 32;

        if (command.Flag("--flat"))
        {
            Renderer.Flat(holder, key, output, maxBytes);
        }
        else
        {
            Renderer.Tree(holder, key.IsRoot ? null : LabelOf(holder, key), output, maxBytes);
        }
    }

    private static async Task GetAsync(Arguments command, TextWriter output)
    {
        command.Expect(2, 2, "get FILE KEY");
        Element root = await LoadAsync(command).ConfigureAwait(false);
        Element holder = Find(root, ConfigKey.Parse(command.Positional(1)));
        string? outFile = command.Value("--out");

        if (outFile != null)
        {
            Element value = CfgLayout.ValueOf(holder) ?? throw new PCfgException(ExitCodes.Failed, "Only a string, a number, a boolean or a BLOB has bytes to write.");
            await File.WriteAllBytesAsync(outFile, value.Data.ToArray()).ConfigureAwait(false);
            return;
        }

        if (command.Flag("--json"))
        {
            output.WriteLine(Renderer.Json(holder));
        }
        else if (CfgLayout.IsContainer(holder))
        {
            Renderer.Tree(holder, null, output, int.MaxValue);
        }
        else
        {
            output.WriteLine(Renderer.Raw(holder));
        }
    }

    private static async Task SetAsync(Arguments command, TextWriter output)
    {
        command.Expect(3, 3, "set FILE KEY VALUE");
        string file = command.Positional(0);
        Element root = await LoadAsync(command).ConfigureAwait(false);
        ConfigKey key = ConfigKey.Parse(command.Positional(1));

        if (key.IsRoot)
        {
            throw new PCfgException(ExitCodes.Failed, "The root is always an object; set its members, or use import.");
        }

        Element container = FindOrCreateContainer(root, key.Parent);
        Element? holder = Child(container, key.Last, key, allowAppend: true);
        Element[] content = ValueParser.Parse(command.Positional(2), holder is null ? null : CfgLayout.TypeOf(holder));

        if (holder is null)
        {
            holder = CfgLayout.TypeOf(container) == CfgLayout.Object ? CfgLayout.NameNode(key.Last) : CfgLayout.ItemNode();
            container.Add(holder);
        }

        CfgLayout.Fill(holder, content);
        string? comment = command.Value("--comment");

        if (comment != null)
        {
            CfgLayout.SetComments(holder, [comment]);
        }

        await SaveAsync(root, command.Value("--output") ?? file).ConfigureAwait(false);
        output.WriteLine($"set {key}");
    }

    private static async Task DeleteAsync(Arguments command, TextWriter output)
    {
        command.Expect(2, 2, "delete FILE KEY");
        string file = command.Positional(0);
        Element root = await LoadAsync(command).ConfigureAwait(false);
        ConfigKey key = ConfigKey.Parse(command.Positional(1));

        if (key.IsRoot)
        {
            throw new PCfgException(ExitCodes.Failed, "The root can not be deleted (create a new file instead).");
        }

        Element holder = Find(root, key);
        Element container = holder.Parent!;

        // Every member with the name: otherwise an earlier one would come back
        IEnumerable<Element> doomed = CfgLayout.Tag(holder) == CfgLayout.NameTag
            ? CfgLayout.Members(container).Where(member => CfgLayout.NameOf(member) == key.Last).ToList()
            : [holder];

        foreach (Element node in doomed)
        {
            container.Remove(node);
        }

        await SaveAsync(root, command.Value("--output") ?? file).ConfigureAwait(false);
        output.WriteLine($"deleted {key}");
    }

    private static async Task RenameAsync(Arguments command, TextWriter output)
    {
        command.Expect(3, 3, "rename FILE KEY NEW-NAME");
        string file = command.Positional(0);
        Element root = await LoadAsync(command).ConfigureAwait(false);
        ConfigKey key = ConfigKey.Parse(command.Positional(1));
        string name = command.Positional(2);
        Element holder = Find(root, key);

        if (CfgLayout.Tag(holder) != CfgLayout.NameTag)
        {
            throw new PCfgException(ExitCodes.Failed, $"{key.Display} is not a member of an object, it has no name.");
        }

        if (CfgLayout.Member(holder.Parent!, name) is { } existing && existing != holder)
        {
            throw new PCfgException(ExitCodes.Failed, $"{key.Parent.Child(name).Display} already exists (delete it first).");
        }

        holder.Data = Encoding.UTF8.GetBytes(name);

        await SaveAsync(root, command.Value("--output") ?? file).ConfigureAwait(false);
        output.WriteLine($"renamed {key} to {key.Parent.Child(name)}");
    }

    private static async Task CommentAsync(Arguments command, TextWriter output)
    {
        command.Expect(2, 3, "comment FILE KEY [TEXT]");
        string file = command.Positional(0);
        Element root = await LoadAsync(command).ConfigureAwait(false);
        ConfigKey key = ConfigKey.Parse(command.Positional(1));
        Element holder = Find(root, key);
        string? text = command.Count > 2 ? command.Positional(2) : null;

        if (command.Flag("--add") && text is null)
        {
            throw new PCfgException(ExitCodes.Usage, "--add needs a TEXT.");
        }

        var comments = command.Flag("--add") ? CfgLayout.Comments(holder).ToList() : [];

        if (text != null)
        {
            comments.Add(text);
        }

        CfgLayout.SetComments(holder, comments);

        await SaveAsync(root, command.Value("--output") ?? file).ConfigureAwait(false);
        output.WriteLine($"{(text is null ? "uncommented" : "commented")} {key.Display}");
    }

    private static async Task ImportAsync(Arguments command, TextReader input, TextWriter output)
    {
        command.Expect(2, 2, "import FILE JSON-FILE|-");
        string file = command.Positional(0);
        string source = command.Positional(1);
        EnsureNew(file, command);

        string json;

        try
        {
            json = source == "-" ? await input.ReadToEndAsync().ConfigureAwait(false) : await File.ReadAllTextAsync(source).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new PCfgException(ExitCodes.NotFound, $"Can not read {source}: {ex.Message}");
        }

        Element root = NewRoot(command);
        root.Add(ValueParser.RootFromJson(json, source == "-" ? "the standard input" : source));

        await SaveAsync(root, file).ConfigureAwait(false);
        output.WriteLine($"imported {file}");
    }

    private static async Task ExportAsync(Arguments command, TextWriter output)
    {
        command.Expect(1, 2, "export FILE [KEY]");
        Element root = await LoadAsync(command).ConfigureAwait(false);
        Element holder = Find(root, command.Count > 1 ? ConfigKey.Parse(command.Positional(1)) : ConfigKey.Root);
        string json = Renderer.Json(holder);
        string? outFile = command.Value("--output");

        if (outFile is null)
        {
            output.WriteLine(json);
            return;
        }

        try
        {
            await File.WriteAllTextAsync(outFile, json + "\n").ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new PCfgException(ExitCodes.Failed, $"Can not write {outFile}: {ex.Message}");
        }
    }

    private static async Task<int> CheckAsync(Arguments command, TextWriter output, TextWriter error)
    {
        command.Expect(1, 1, "check FILE");
        string file = command.Positional(0);
        Element root = await LoadTreeAsync(file).ConfigureAwait(false);
        List<Problem> problems = Validator.Check(root, Signature(command));

        foreach (Problem problem in problems)
        {
            error.WriteLine($"pcfg: {problem}");
        }

        int errors = problems.Count(problem => problem.IsError);
        int warnings = problems.Count - errors;
        output.WriteLine(string.Create(CultureInfo.InvariantCulture, $"{file}: {errors} errors, {warnings} warnings"));

        return errors > 0 || (warnings > 0 && command.Flag("--strict")) ? ExitCodes.Failed : ExitCodes.Success;
    }

    #endregion

    #region Helpers

    private static string Signature(Arguments command)
    {
        string signature = command.Value("--signature") ?? CfgLayout.ConfigurationSignature;

        if (signature.Length is 0 or > ElementBuilder.MaxSignatureLength || !signature.All(c => c is >= ' ' and <= '~'))
        {
            throw new PCfgException(ExitCodes.Usage, $"Invalid signature '{signature}': 1 to {ElementBuilder.MaxSignatureLength} printable ASCII characters.");
        }

        return signature;
    }

    private static Element NewRoot(Arguments command) => new ElementBuilder().WithSignature(Encoding.ASCII.GetBytes(Signature(command))).Build();

    private static void EnsureNew(string file, Arguments command)
    {
        if (File.Exists(file) && !command.Flag("--force"))
        {
            throw new PCfgException(ExitCodes.Failed, $"{file} already exists (use --force to overwrite it).");
        }
    }

    /// <summary>Loads a configuration and checks it: an invalid file is not shown or edited.</summary>
    private static async Task<Element> LoadAsync(Arguments command)
    {
        string file = command.Positional(0);
        Element root = await LoadTreeAsync(file).ConfigureAwait(false);
        Problem? problem = Validator.Check(root, Signature(command)).FirstOrDefault(problem => problem.IsError);

        if (problem != null)
        {
            throw new PCfgException(ExitCodes.Failed, $"{file} is not a valid configuration: {problem.Key}: {problem.Message} (run 'pcfg check {file}' for every problem).");
        }

        return root;
    }

    private static async Task<Element> LoadTreeAsync(string file)
    {
        try
        {
            return await Element.LoadAsync(file).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            throw new PCfgException(ExitCodes.NotFound, $"{file} does not exist.");
        }
        catch (PromptuariumException ex)
        {
            throw new PCfgException(ExitCodes.Failed, $"{file} is not a valid Promptuarium stream: {ex.Message}");
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
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new PCfgException(ExitCodes.Failed, $"Can not save {file}: {ex.Message}");
        }
        finally
        {
            File.Delete(temporary);
        }
    }

    /// <summary>Finds the holder of a key (the root for the empty key).</summary>
    private static Element Find(Element root, ConfigKey key)
    {
        Element current = root;
        ConfigKey walked = ConfigKey.Root;

        foreach (string segment in key.Segments)
        {
            if (!CfgLayout.IsContainer(current))
            {
                throw new PCfgException(ExitCodes.NotFound, $"{key.Display} does not exist: {walked.Display} is {CfgLayout.TypeName(CfgLayout.TypeOf(current))}, not an object or an array.");
            }

            current = Child(current, segment, key, allowAppend: false)
                ?? throw new PCfgException(ExitCodes.NotFound, $"{key.Display} does not exist: {walked.Display} has no {(CfgLayout.TypeOf(current) == CfgLayout.Object ? "member" : "element")} '{segment}'.");
            walked = walked.Child(segment);
        }

        return current;
    }

    /// <summary>Finds the object or the array of a key, creating the missing objects on the way.</summary>
    private static Element FindOrCreateContainer(Element root, ConfigKey key)
    {
        Element current = root;
        ConfigKey walked = ConfigKey.Root;

        foreach (string segment in key.Segments)
        {
            Element? next = Child(current, segment, key, allowAppend: true);

            if (next is null)
            {
                next = CfgLayout.TypeOf(current) == CfgLayout.Object ? CfgLayout.NameNode(segment) : CfgLayout.ItemNode();
                next.Add(CfgLayout.EmptyContent(CfgLayout.Object));
                current.Add(next);
            }

            walked = walked.Child(segment);

            if (!CfgLayout.IsContainer(next))
            {
                throw new PCfgException(ExitCodes.Failed, $"{walked.Display} is {CfgLayout.TypeName(CfgLayout.TypeOf(next))}, so it has no members (set it to {{}} first).");
            }

            current = next;
        }

        return current;
    }

    /// <summary>
    /// The member (the last one with the name) or the element of a container; null if it does not exist. An index of
    /// an array must exist, or (with <paramref name="allowAppend"/>) be the next one.
    /// </summary>
    private static Element? Child(Element container, string segment, ConfigKey key, bool allowAppend)
    {
        if (CfgLayout.TypeOf(container) == CfgLayout.Object)
        {
            return CfgLayout.Member(container, segment);
        }

        IReadOnlyList<Element> items = CfgLayout.Items(container);

        if (!ConfigKey.TryIndex(segment, out int index) || index > items.Count || (index == items.Count && !allowAppend))
        {
            if (!allowAppend)
            {
                return null;
            }

            throw new PCfgException(ExitCodes.Failed, string.Create(CultureInfo.InvariantCulture, $"Invalid key {key.Display}: '{segment}' is not an index of the array, which has {items.Count} elements (0 to {items.Count})."));
        }

        return index < items.Count ? items[index] : null;
    }

    private static string LabelOf(Element holder, ConfigKey key) => CfgLayout.Tag(holder) == CfgLayout.NameTag ? Renderer.Label(key.Last) : key.Last;

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
            bool options = true;

            while (enumerator.MoveNext())
            {
                string arg = enumerator.Current;

                if (options && arg == "--")
                {
                    // Everything after "--" is positional, e.g. a value that starts with "--"
                    options = false;
                }
                else if (options && ValueOptions.Contains(arg))
                {
                    if (!enumerator.MoveNext())
                    {
                        throw new PCfgException(ExitCodes.Usage, $"Option {arg} needs a value.");
                    }

                    values[arg] = enumerator.Current;
                }
                else if (options && FlagOptions.Contains(arg))
                {
                    flags.Add(arg);
                }
                else if (options && arg.StartsWith("--", StringComparison.Ordinal))
                {
                    throw new PCfgException(ExitCodes.Usage, $"Unknown option {arg}.");
                }
                else
                {
                    positionals.Add(arg);
                }
            }
        }

        public int Count => positionals.Count;

        public void Expect(int min, int max, string usage)
        {
            if (positionals.Count < min || positionals.Count > max)
            {
                throw new PCfgException(ExitCodes.Usage, $"Usage: pcfg {usage}");
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
                throw new PCfgException(ExitCodes.Usage, $"Option {name} needs a non-negative number, not '{value}'.");
            }

            return result;
        }
    }
}
