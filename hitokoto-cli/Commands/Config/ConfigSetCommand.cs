using System.ComponentModel;
using hitokoto_cli.Infrastructure;
using hitokoto_cli.Models;
using hitokoto_cli.Services;
using hitokoto_cli.Settings;
using Spectre.Console;
using Spectre.Console.Cli;

namespace hitokoto_cli.Commands.Config;

internal sealed class ConfigSetSettings : ConfigSettings
{
    [CommandArgument(0, "<key>")]
    [Description("配置键名")]
    public string Key { get; set; } = string.Empty;

    [CommandArgument(1, "<value>")]
    [Description("配置值（按键类型解析）")]
    public string Value { get; set; } = string.Empty;
}

/// <summary>Set a config key, type-checking the value before persisting.</summary>
internal sealed class ConfigSetCommand(ConfigModule config, IAnsiConsole stdout, ErrorConsole stderr)
{
    private readonly ConfigModule _config = config;
    private readonly IAnsiConsole _stdout = stdout;
    private readonly ErrorConsole _stderr = stderr;

    public int Execute(CommandContext _, ConfigSetSettings s, CancellationToken _1)
    {
        if (!ConfigModule.TryGetKey(s.Key, out var info))
        {
            _stderr.Console.MarkupLine($"[red]错误：未知键 '{Markup.Escape(s.Key)}'[/]");
            return 2;
        }

        _config.EnsureCreated();
        var cfg = _config.Load();

        if (!info.TryParse(s.Value, out var parsed))
        {
            _stderr.Console.MarkupLine(
                $"[red]错误：值 '{Markup.Escape(s.Value)}' 无法解析为 {Markup.Escape(info.ExpectedTypeDisplay)}[/]");
            return 2;
        }

        info.Setter(cfg, parsed!);
        _config.Save(cfg);
        _stdout.MarkupLine($"[green]已设置[/] {Markup.Escape(s.Key)} = {Markup.Escape(s.Value)}");
        return 0;
    }
}
