using Spectre.Console.Cli;

namespace hitokoto_cli.Settings;

/// <summary>Base settings for the <c>config</c> branch. Currently holds no
/// shared options; subcommands derive their own settings from this.</summary>
internal class ConfigSettings : CommandSettings
{
}
