using hitokoto_cli.Services;
using hitokoto_cli.Settings;
using Spectre.Console;
using Spectre.Console.Cli;

namespace hitokoto_cli.Commands.Config;

/// <summary>Print the config file path (whether or not it exists).</summary>
internal sealed class ConfigPathCommand(IAnsiConsole stdout)
{
    private readonly IAnsiConsole _stdout = stdout;

    public int Execute(CommandContext _, ConfigSettings _1, CancellationToken _2)
    {
        _stdout.WriteLine(ConfigStore.GetFilePath());
        return 0;
    }
}
