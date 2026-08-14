using System.ComponentModel;
using hitokoto_cli.Infrastructure;
using hitokoto_cli.Services;
using hitokoto_cli.Settings;
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
internal sealed class ConfigSetCommand(ConfigModule config, Diagnostics diagnostics)
{
    private readonly ConfigModule _config = config;
    private readonly Diagnostics _diagnostics = diagnostics;

    public int Execute(CommandContext _, ConfigSetSettings s, CancellationToken _1)
    {
        if (!ConfigModule.TryGetKey(s.Key, out var info))
        {
            return _diagnostics.UsageError($"未知键 '{s.Key}'");
        }

        _config.EnsureCreated();
        var cfg = _config.Load();

        if (!info.TryParse(s.Value, out var parsed))
        {
            return _diagnostics.UsageError($"值 '{s.Value}' 无法解析为 {info.ExpectedTypeDisplay}");
        }

        info.Setter(cfg, parsed!);
        _config.Save(cfg);
        _diagnostics.Success("已设置", $"{s.Key} = {s.Value}");
        return 0;
    }
}
