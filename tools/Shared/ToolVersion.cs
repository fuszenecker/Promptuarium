using System.Reflection;

namespace Promptuarium.Tools;

/// <summary>
/// The version of a tool: the version of the packages (src/Directory.Build.props, or -p:Version of a release build)
/// and the commit it was built from.
/// </summary>
internal static class ToolVersion
{
    private static readonly string Informational =
        typeof(ToolVersion).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? typeof(ToolVersion).Assembly.GetName().Version?.ToString(3)
        ?? "0.0.0";

    /// <summary>The version without the commit, e.g. 8.4.0 or 8.5.0-rc1.</summary>
    public static string Version => Informational.Split('+')[0];

    /// <summary>The abbreviated hash of the commit, or null if the build does not know it.</summary>
    public static string? Commit => Informational.Split('+') is [_, string hash, ..] && hash.Length > 0
        ? hash[..System.Math.Min(hash.Length, 7)]
        : null;

    /// <summary>The line of --version, e.g. "pcfg 8.4.0 (4c18335)".</summary>
    public static string Line(string toolName) => Commit is string commit
        ? $"{toolName} {Version} ({commit})"
        : $"{toolName} {Version}";
}
