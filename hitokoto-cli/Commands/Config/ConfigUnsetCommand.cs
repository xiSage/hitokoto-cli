using System.ComponentModel;
using hitokoto_cli.Infrastructure;
using hitokoto_cli.Models;
using hitokoto_cli.Services;
using hitokoto_cli.Settings;
using Spectre.Console;
using Spectre.Console.Cli;

namespace hitokoto_cli.Commands.Config;

internal sealed class ConfigUnsetSettings : ConfigSettings
{
    [CommandArgument(0, "<key>")]
    [Description("配置键名")]
    public string Key { get; set; } = string.Empty;
}

/// <summary>Clear a config key (set to null, falls back to default at use time).</summary>
internal sealed class ConfigUnsetCommand(IConfigStore configStore, IAnsiConsole stdout, ErrorConsole stderr)
{
    private readonly IConfigStore _configStore = configStore;
    private readonly IAnsiConsole _stdout = stdout;
    private readonly ErrorConsole _stderr = stderr;

    public int Execute(CommandContext _, ConfigUnsetSettings s, CancellationToken _1)
    {
        if (!ConfigKeys.TryGet(s.Key, out var info))
        {
            _stderr.Console.MarkupLine($"[red]错误：未知键 '{Markup.Escape(s.Key)}'[/]");
            return 2;
        }

        if (!_configStore.FileExists)
        {
            _stderr.Console.MarkupLine("[yellow]无配置文件[/]");
            return 0;
        }

        var cfg = _configStore.Load();
        info.Clearer(cfg);
        _configStore.Save(cfg);
        _stdout.MarkupLine($"[green]已清除[/] {Markup.Escape(s.Key)}");
        return 0;
    }
}
