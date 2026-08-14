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
var diagnostics = new Diagnostics(stderr);

// Construct services eagerly. Command classes are kept as plain workers that
// the delegates below dispatch into. This avoids Spectre.Console.Cli's
// reflection-based FromType<TCommand> path (which breaks under Native AOT),
// while still letting Spectre bind settings and render help/tables.
var configModule = new ConfigModule(ConfigModule.GetDefaultFilePath(), diagnostics);
var hitokotoClient = new HitokotoClient(diagnostics);

var defaultCommand = new DefaultCommand(hitokotoClient, configModule, stdout, diagnostics);
var configListCommand = new ConfigListCommand(configModule, stdout, diagnostics);
var configGetCommand = new ConfigGetCommand(configModule, stdout, diagnostics);
var configSetCommand = new ConfigSetCommand(configModule, diagnostics);
var configUnsetCommand = new ConfigUnsetCommand(configModule, diagnostics);
var configPathCommand = new ConfigPathCommand(configModule, stdout);
var configResetCommand = new ConfigResetCommand(configModule);

var registrar = new DefaultTypeRegistrar();
registrar.RegisterInstance(typeof(IAnsiConsole), stdout);

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

    config.SetExceptionHandler((ex, _) => diagnostics.HandleException(ex));

    // Hidden default command: dispatches to DefaultCommand. Routes here when
    // the user passes no subcommand (args empty or all options).
    config.AddAsyncDelegate<FetchSettings>("__default__",
        (ctx, s, ct) => defaultCommand.ExecuteAsync(ctx, s, ct))
        .IsHidden()
        .WithDescription("获取一言（默认行为）");

    config.AddBranch<ConfigSettings>("config", branch =>
    {
        Register<ConfigSettings>(branch, CliSurface.ConfigList, (ctx, s, ct) => configListCommand.Execute(ctx, s, ct));
        Register<ConfigGetSettings>(branch, CliSurface.ConfigGet, (ctx, s, ct) => configGetCommand.Execute(ctx, s, ct));
        Register<ConfigSetSettings>(branch, CliSurface.ConfigSet, (ctx, s, ct) => configSetCommand.Execute(ctx, s, ct));
        Register<ConfigUnsetSettings>(branch, CliSurface.ConfigUnset, (ctx, s, ct) => configUnsetCommand.Execute(ctx, s, ct));
        Register<ConfigSettings>(branch, CliSurface.ConfigPath, (ctx, s, ct) => configPathCommand.Execute(ctx, s, ct));
        Register<ConfigSettings>(branch, CliSurface.ConfigReset, (ctx, s, ct) => configResetCommand.Execute(ctx, s, ct));
    });
});

// Top-level version: when --version/-v appears, print version and exit.
if (CliSurface.ShouldShowVersion(args))
{
    CliSurface.PrintVersion();
    return 0;
}

// Top-level help: when --help/-h appears before any subcommand, print a
// comprehensive custom help (Spectre's auto-help would expose the hidden
// __default__ command name and fragment the information across commands).
if (CliSurface.ShouldShowHelp(args))
{
    HelpPrinter.Print(stdout);
    return 0;
}

// Route args: if no subcommand is given, run the hidden default command.
// `hitokoto config` alone is treated as `hitokoto config list`.
var effectiveArgs = CliSurface.ResolveArgs(args);

// See CliSurface.SkipInternalCliBranch for why Spectre's hidden "cli" branch
// must be bypassed under Native AOT.
CliSurface.SkipInternalCliBranch(app);

return app.Run(effectiveArgs);

// Registers a config subcommand from its CommandInfo row: name, aliases and
// description all come from the table (single source of truth), only the
// settings type and handler are given here. Explicit generic AddDelegate keeps
// the settings types compile-time known (AOT-safe — no reflection).
static void Register<T>(IConfigurator<ConfigSettings> branch, CommandInfo info,
    Func<CommandContext, T, CancellationToken, int> handler)
    where T : ConfigSettings
{
    var command = branch.AddDelegate<T>(info.Name, handler).WithDescription(info.Description);
    foreach (var alias in info.Aliases)
    {
        command.WithAlias(alias);
    }
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
