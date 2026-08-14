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
internal sealed class ConfigGetCommand(ConfigModule config, IAnsiConsole stdout, Diagnostics diagnostics)
{
    private readonly ConfigModule _config = config;
    private readonly IAnsiConsole _stdout = stdout;
    private readonly Diagnostics _diagnostics = diagnostics;

    public int Execute(CommandContext _, ConfigGetSettings s, CancellationToken _1)
    {
        if (!ConfigModule.TryGetKey(s.Key, out var info))
        {
            return _diagnostics.UsageError($"未知键 '{s.Key}'");
        }

        if (!_config.FileExists)
        {
            _diagnostics.Warn("未找到配置文件，显示默认值");
        }

        var cfg = _config.Load();
        _stdout.WriteLine(ConfigModule.FormatValue(info.Getter(cfg)));
        return 0;
    }
}
