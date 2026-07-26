using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using hitokoto_cli;
using hitokoto_cli.Commands;
using hitokoto_cli.Commands.Config;
using hitokoto_cli.Infrastructure;
using hitokoto_cli.Services;
using hitokoto_cli.Settings;
using Spectre.Console;
using Spectre.Console.Cli;

// InvariantGlobalization does not affect UTF-8 encoding, but the Windows
// console default codepage is not UTF-8 — force it so Chinese text renders.
Console.OutputEncoding = Encoding.UTF8;

// Keep settings-type property metadata alive under Native AOT trimming.
// See AotPreservation.EnsureSettingsTypesPreserved for why this is needed.
AotPreservation.EnsureSettingsTypesPreserved();

var stdout = AnsiConsole.Console;
var stderr = AnsiConsole.Create(new AnsiConsoleSettings
{
    Out = new AnsiConsoleOutput(Console.Error),
});
var errorConsole = new ErrorConsole(stderr);

// Construct services eagerly. Command classes are kept as plain workers that
// the delegates below dispatch into. This avoids Spectre.Console.Cli's
// reflection-based FromType<TCommand> path (which breaks under Native AOT),
// while still letting Spectre bind settings and render help/tables.
var configStore = new ConfigStore(errorConsole);
var hitokotoClient = new HitokotoClient(errorConsole);
var formatter = new OutputFormatter();

var defaultCommand = new DefaultCommand(hitokotoClient, configStore, stdout, errorConsole, formatter);
var configListCommand = new ConfigListCommand(configStore, stdout, errorConsole);
var configGetCommand = new ConfigGetCommand(configStore, stdout, errorConsole);
var configSetCommand = new ConfigSetCommand(configStore, stdout, errorConsole);
var configUnsetCommand = new ConfigUnsetCommand(configStore, stdout, errorConsole);
var configPathCommand = new ConfigPathCommand(stdout);
var configResetCommand = new ConfigResetCommand(configStore);

var registrar = new DefaultTypeRegistrar();
registrar.RegisterInstance(typeof(IAnsiConsole), stdout);
registrar.RegisterInstance(typeof(ErrorConsole), errorConsole);

// Pre-register settings types with explicit factories so Spectre can resolve
// them without Activator.CreateInstance (which needs reflection — trimmed
// under AOT). RegisterLazy pins the entry, so Spectre's later
// Register(settingsType, settingsType) calls are ignored.
registrar.RegisterLazy(typeof(FetchSettings), () => new FetchSettings());
registrar.RegisterLazy(typeof(ConfigSettings), () => new ConfigSettings());
registrar.RegisterLazy(typeof(ConfigGetSettings), () => new ConfigGetSettings());
registrar.RegisterLazy(typeof(ConfigSetSettings), () => new ConfigSetSettings());
registrar.RegisterLazy(typeof(ConfigUnsetSettings), () => new ConfigUnsetSettings());

// Non-generic CommandApp (no SetDefaultCommand<T> — that path uses reflection
// to discover ICommand<TSettings> via GetInterfaces(), which Native AOT does
// not support). Instead the default fetch behavior is registered as a hidden
// delegate command and args are rewritten to route to it.
var app = CommandAppFactory.Create(registrar);

app.Configure(config =>
{
    config.Settings.ApplicationName = "hitokoto";
    config.Settings.ShowOptionDefaultValues = true;
    config.UseStrictParsing();

    config.SetExceptionHandler((ex, resolver) =>
    {
        var err = resolver?.Resolve(typeof(ErrorConsole)) as ErrorConsole;
        var console = err?.Console ?? AnsiConsole.Console;
        var code = ex is IOException or UnauthorizedAccessException ? 3 : 1;
        console.MarkupLine($"[red]错误：{Markup.Escape(ex.Message)}[/]");
        return code;
    });

    // Hidden default command: dispatches to DefaultCommand. Routes here when
    // the user passes no subcommand (args empty or all options).
    config.AddAsyncDelegate<FetchSettings>("__default__",
        (ctx, s, ct) => defaultCommand.ExecuteAsync(ctx, s, ct))
        .IsHidden()
        .WithDescription("获取一言（默认行为）");

    config.AddBranch<ConfigSettings>("config", branch =>
    {
        branch.AddDelegate<ConfigSettings>("list",
            (ctx, s, ct) => configListCommand.Execute(ctx, s, ct))
            .WithDescription("列出全部配置值");
        branch.AddDelegate<ConfigGetSettings>("get",
            (ctx, s, ct) => configGetCommand.Execute(ctx, s, ct))
            .WithDescription("获取配置值");
        branch.AddDelegate<ConfigSetSettings>("set",
            (ctx, s, ct) => configSetCommand.Execute(ctx, s, ct))
            .WithDescription("设置配置值");
        branch.AddDelegate<ConfigUnsetSettings>("unset",
            (ctx, s, ct) => configUnsetCommand.Execute(ctx, s, ct))
            .WithDescription("清除配置值（恢复默认）");
        branch.AddDelegate<ConfigSettings>("path",
            (ctx, s, ct) => configPathCommand.Execute(ctx, s, ct))
            .WithDescription("显示配置文件路径");
        branch.AddDelegate<ConfigSettings>("reset",
            (ctx, s, ct) => configResetCommand.Execute(ctx, s, ct))
            .WithDescription("重置配置文件为默认值");
    });
});

// Top-level version: when --version/-v appears, print version and exit.
if (IsVersionRequest(args))
{
    PrintVersion();
    return 0;
}

// Top-level help: when --help/-h appears before any subcommand, print a
// comprehensive custom help (Spectre's auto-help would expose the hidden
// __default__ command name and fragment the information across commands).
if (IsTopLevelHelpRequest(args))
{
    HelpPrinter.Print(stdout);
    return 0;
}

// Route args: if no subcommand is given, run the hidden default command.
// `hitokoto config` alone is treated as `hitokoto config list`.
var effectiveArgs = ResolveEffectiveArgs(args);

// Spectre.Console.Cli's RunAsync unconditionally registers a hidden "cli"
// branch (version / xmldoc / explain) via AddCommand<T> — that path uses
// ConfigurationHelper.GetSettingsType, which reflects over ICommand<TSettings>
// via Type.GetInterfaces(). Native AOT does not surface closed-generic
// interfaces through GetInterfaces(), so registration throws
// "Could not get settings type for command of type 'VersionCommand'". Setting
// the internal _executed flag beforehand makes RunAsync skip that branch.
// The hidden version/xmldoc/explain commands are not part of the public CLI
// surface and are not needed.
SkipInternalCliBranch(app);

return app.Run(effectiveArgs);

static void SkipInternalCliBranch(CommandApp app)
{
    var field = typeof(CommandApp).GetField("_executed",
        BindingFlags.NonPublic | BindingFlags.Instance);
    field?.SetValue(app, true);
}

static string[] ResolveEffectiveArgs(string[] args)
{
    // Empty or option-only → default fetch command.
    if (args.Length == 0 || args[0].StartsWith('-'))
    {
        return ["__default__", .. args];
    }

    // `config` with no subcommand → `config list` (the branch default).
    if (args.Length == 1 && args[0] == "config")
    {
        return ["config", "list"];
    }

    return args;
}

static bool IsVersionRequest(string[] args)
{
    foreach (var a in args)
    {
        if (a is "--version" or "-v")
        {
            return true;
        }
    }

    return false;
}

static void PrintVersion()
{
    var version = typeof(Program).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
        .InformationalVersion ?? "unknown";
    // 去除 git commit hash 后缀 (如 +abc123)
    var plus = version.IndexOf('+');
    if (plus >= 0) version = version[..plus];
    Console.WriteLine(version);
}

static bool IsTopLevelHelpRequest(string[] args)
{
    // --help/-h before any subcommand (i.e. first arg starts with '-').
    if (args.Length == 0 || !args[0].StartsWith('-'))
    {
        return false;
    }

    foreach (var a in args)
    {
        if (a is "--help" or "-h")
        {
            return true;
        }
    }

    return false;
}

/// <summary>
/// Wraps <c>new CommandApp(registrar)</c> so the IL3050 warning from
/// Spectre's <c>[RequiresDynamicCode]</c>-annotated constructor can be
/// suppressed in one place. Safe because Spectre.Console(.Cli) is preserved
/// wholesale via TrimmerRoots.xml and command registration uses the
/// reflection-free <c>AddDelegate</c>/<c>AddAsyncDelegate</c> API.
/// </summary>
internal static class CommandAppFactory
{
    [UnconditionalSuppressMessage("AOT", "IL3050",
        Justification = "Spectre.Console.Cli relies on reflection internally, but the Spectre assemblies are preserved wholesale via TrimmerRoots.xml, and command registration uses AddDelegate/AddAsyncDelegate (avoiding the reflection-based AddCommand<T> path).")]
    public static CommandApp Create(ITypeRegistrar registrar) => new(registrar);
}
