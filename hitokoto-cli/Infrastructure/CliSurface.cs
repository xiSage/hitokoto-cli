using System.Reflection;
using Spectre.Console.Cli;

namespace hitokoto_cli.Infrastructure;

/// <summary>
/// Display spec of a CLI command: name, aliases, description, and any argument
/// placeholders shown in help. Single source of truth for the config
/// subcommands — both Program's registration and HelpPrinter read this table,
/// so names/aliases/descriptions cannot drift apart.
/// </summary>
internal sealed record CommandInfo(
    string Name,
    string[] Aliases,
    string Description,
    string? Args = null)
{
    /// <summary>Help rendering of name + aliases + argument placeholders.</summary>
    public string UsageSpec => Args is null
        ? (Aliases.Length > 0 ? $"{Name}, {string.Join(", ", Aliases)}" : Name)
        : $"{Name}{(Aliases.Length > 0 ? $", {string.Join(", ", Aliases)}" : string.Empty)} {Args}";
}

/// <summary>
/// The CLI surface: top-level arg routing (version/help/default-command),
/// the config command table, and the option spec/description constants.
/// Program and HelpPrinter both read from here so aliases, options, and help
/// text come from one place. Also hosts the AOT workaround for Spectre's
/// hidden "cli" branch.
/// </summary>
internal static class CliSurface
{
    public static class Options
    {
        // Option templates and descriptions, shared by the FetchSettings
        // attributes and HelpPrinter. Specs use '|' as short/long separator;
        // help renders it as ", ".
        public const string CategorySpec = "-c|--category <CATEGORY>";
        public const string CategoryDesc = "句子分类 (a-l)，可多次指定；缺省则不限制";
        public const string MinLengthSpec = "--min-length <N>";
        public const string MinLengthDesc = "句子最小长度（含）；缺省则不限制";
        public const string MaxLengthSpec = "--max-length <N>";
        public const string MaxLengthDesc = "句子最大长度（含）；缺省则不限制";
        public const string EndpointSpec = "--endpoint <URL>";
        public const string EndpointDesc = "API 端点 URL";
        public const string FormatSpec = "-f|--format <FORMAT>";
        public const string FormatDesc = "CLI 输出格式: text | json | full（默认 full）";
        public const string RawSpec = "-r|--raw <ENCODE>";
        public const string RawDesc = "透传 API 响应: text | json（与 --format 互斥）";
        public const string ShowSourceSpec = "--show-source <TRUE_FALSE>";
        public const string ShowSourceDesc = "full 格式是否显示来源（默认 true）";
        public const string ShowLinkSpec = "--show-link <TRUE_FALSE>";
        public const string ShowLinkDesc = "full 格式是否显示链接（默认 true）";
        public const string NoConfigSpec = "-n|--no-config";
        public const string NoConfigDesc = "忽略配置文件，使用内置默认值";
        public const string HelpSpec = "-h|--help";
        public const string HelpDesc = "显示此帮助信息";
        public const string VersionSpec = "-v|--version";
        public const string VersionDesc = "显示版本信息";

        /// <summary>Renders an option template for help: '|' → ', ', with a
        /// 4-space indent for long-only options so columns align.</summary>
        public static string RenderSpec(string spec)
        {
            var display = spec.Replace("|", ", ");
            return display.StartsWith('-') && !display.Contains(',') ? "    " + display : display;
        }

        /// <summary>Whether any arg matches one of the spec's short/long tokens.</summary>
        public static bool HasOption(string[] args, string spec)
            => args.Any(a => spec.Split('|').Contains(a));
    }

    public static readonly CommandInfo ConfigList = new("list", ["ls"], "列出全部配置值");
    public static readonly CommandInfo ConfigGet = new("get", [], "获取配置值", "<key>");
    public static readonly CommandInfo ConfigSet = new("set", [], "设置配置值（校验类型）", "<key> <value>");
    public static readonly CommandInfo ConfigUnset = new("unset", ["rm"], "清除配置值（恢复默认）", "<key>");
    public static readonly CommandInfo ConfigPath = new("path", [], "显示配置文件路径");
    public static readonly CommandInfo ConfigReset = new("reset", [], "重置配置文件为默认值");

    public static readonly IReadOnlyList<CommandInfo> ConfigCommands =
        [ConfigList, ConfigGet, ConfigSet, ConfigUnset, ConfigPath, ConfigReset];

    public static bool ShouldShowVersion(string[] args)
        => Options.HasOption(args, Options.VersionSpec);

    public static bool ShouldShowHelp(string[] args)
        => args.Length > 0 && args[0].StartsWith('-') && Options.HasOption(args, Options.HelpSpec);

    /// <summary>
    /// Rewrites args so the hidden default command / branch default get routed:
    /// empty or option-only → <c>__default__</c> (fetch), bare <c>config</c> →
    /// <c>config list</c>.
    /// </summary>
    public static string[] ResolveArgs(string[] args)
    {
        if (args.Length == 0 || args[0].StartsWith('-'))
        {
            return ["__default__", .. args];
        }

        if (args.Length == 1 && args[0] == "config")
        {
            return ["config", "list"];
        }

        return args;
    }

    public static void PrintVersion()
    {
        var version = typeof(CliSurface).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? "unknown";
        // 去除 git commit hash 后缀 (如 +abc123)
        var plus = version.IndexOf('+');
        if (plus >= 0) version = version[..plus];
        Console.WriteLine(version);
    }

    /// <summary>
    /// Spectre.Console.Cli's RunAsync unconditionally registers a hidden "cli"
    /// branch (version / xmldoc / explain) via AddCommand&lt;T&gt; — that path uses
    /// ConfigurationHelper.GetSettingsType, which reflects over ICommand&lt;TSettings&gt;
    /// via Type.GetInterfaces(). Native AOT does not surface closed-generic
    /// interfaces through GetInterfaces(), so registration throws. Setting the
    /// internal _executed flag beforehand makes RunAsync skip that branch. The
    /// hidden version/xmldoc/explain commands are not part of the public CLI.
    /// </summary>
    public static void SkipInternalCliBranch(CommandApp app)
    {
        var field = typeof(CommandApp).GetField("_executed",
            BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(app, true);
    }
}
