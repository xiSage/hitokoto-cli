using hitokoto_cli.Services;
using hitokoto_cli.Settings;
using Spectre.Console.Cli;

namespace hitokoto_cli.Commands.Config;

/// <summary>Reset the config file to built-in defaults.</summary>
internal sealed class ConfigResetCommand(IConfigStore configStore)
{
    private readonly IConfigStore _configStore = configStore;

    public int Execute(CommandContext _, ConfigSettings _1, CancellationToken _2)
    {
        _configStore.Reset();
        return 0;
    }
}
