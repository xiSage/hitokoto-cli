using System.ComponentModel;
using hitokoto_cli.Infrastructure;
using hitokoto_cli.Models;
using hitokoto_cli.Services;
using hitokoto_cli.Settings;
using Spectre.Console;
using Spectre.Console.Cli;

namespace hitokoto_cli.Commands.Config;

internal sealed class ConfigGetSettings : ConfigSettings
{
    [CommandArgument(0, "<key>")]
    [Description("配置键名")]
    public string Key { get; set; } = string.Empty;
}

/// <summary>Print the current value of a single config key.</summary>
internal sealed class ConfigGetCommand(IConfigStore configStore, IAnsiConsole stdout, ErrorConsole stderr)
{
    private readonly IConfigStore _configStore = configStore;
    private readonly IAnsiConsole _stdout = stdout;
    private readonly ErrorConsole _stderr = stderr;

    public int Execute(CommandContext _, ConfigGetSettings s, CancellationToken _1)
    {
        if (!ConfigKeys.TryGet(s.Key, out var info))
        {
            _stderr.Console.MarkupLine($"[red]错误：未知键 '{Markup.Escape(s.Key)}'[/]");
            return 2;
        }

        if (!_configStore.FileExists)
        {
            _stderr.Console.MarkupLine("[yellow]未找到配置文件，显示默认值[/]");
        }

        var cfg = _configStore.Load();
        _stdout.WriteLine(ConfigKeys.FormatValue(info.Getter(cfg)));
        return 0;
    }
}
