using hitokoto_cli.Services;
using hitokoto_cli.Settings;
using Spectre.Console.Cli;

namespace hitokoto_cli.Commands.Config;

/// <summary>Reset the config file to built-in defaults.</summary>
internal sealed class ConfigResetCommand(ConfigModule config)
{
    private readonly ConfigModule _config = config;

    public int Execute(CommandContext _, ConfigSettings _1, CancellationToken _2)
    {
        _config.Reset();
        return 0;
    }
}
