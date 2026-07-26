using hitokoto_cli.Infrastructure;
using hitokoto_cli.Models;
using hitokoto_cli.Services;
using hitokoto_cli.Settings;
using Spectre.Console;
using Spectre.Console.Cli;

namespace hitokoto_cli.Commands.Config;

/// <summary>Branch default: list all config keys and their current values.</summary>
internal sealed class ConfigListCommand(IConfigStore configStore, IAnsiConsole stdout, ErrorConsole stderr)
{
    private readonly IConfigStore _configStore = configStore;
    private readonly IAnsiConsole _stdout = stdout;
    private readonly ErrorConsole _stderr = stderr;

    public int Execute(CommandContext _, ConfigSettings _1, CancellationToken _2)
    {
        if (!_configStore.FileExists)
        {
            _stderr.Console.MarkupLine("[yellow]未找到配置文件，显示默认值[/]");
        }

        var cfg = _configStore.Load();

        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn("键");
        table.AddColumn("值");

        foreach (var (key, info) in ConfigKeys.All)
        {
            var value = info.Getter(cfg);
            var cell = value is null
                ? "[dim](未设置)[/]"
                : Markup.Escape(ConfigKeys.FormatValue(value));
            table.AddRow(Markup.Escape(key), cell);
        }

        _stdout.Write(table);
        return 0;
    }
}
