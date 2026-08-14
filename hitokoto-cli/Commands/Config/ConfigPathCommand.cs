using hitokoto_cli.Services;
using hitokoto_cli.Settings;
using Spectre.Console;
using Spectre.Console.Cli;

namespace hitokoto_cli.Commands.Config;

/// <summary>Print the config file path (whether or not it exists).</summary>
internal sealed class ConfigPathCommand(ConfigModule config, IAnsiConsole stdout)
{
    private readonly ConfigModule _config = config;
    private readonly IAnsiConsole _stdout = stdout;

    public int Execute(CommandContext _, ConfigSettings _1, CancellationToken _2)
    {
        _stdout.WriteLine(_config.Path);
        return 0;
    }
}
