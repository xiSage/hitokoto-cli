using System.ComponentModel;
using hitokoto_cli.Infrastructure;
using hitokoto_cli.Services;
using hitokoto_cli.Settings;
using Spectre.Console.Cli;

namespace hitokoto_cli.Commands.Config;

internal sealed class ConfigUnsetSettings : ConfigSettings
{
    [CommandArgument(0, "<key>")]
    [Description("配置键名")]
    public string Key { get; set; } = string.Empty;
}

/// <summary>Clear a config key (set to null, falls back to default at use time).</summary>
internal sealed class ConfigUnsetCommand(ConfigModule config, Diagnostics diagnostics)
{
    private readonly ConfigModule _config = config;
    private readonly Diagnostics _diagnostics = diagnostics;

    public int Execute(CommandContext _, ConfigUnsetSettings s, CancellationToken _1)
    {
        if (!ConfigModule.TryGetKey(s.Key, out var info))
        {
            return _diagnostics.UsageError($"未知键 '{s.Key}'");
        }

        if (!_config.FileExists)
        {
            _diagnostics.Warn("无配置文件");
            return 0;
        }

        var cfg = _config.Load();
        info.Clearer(cfg);
        _config.Save(cfg);
        _diagnostics.Success("已清除", s.Key);
        return 0;
    }
}
