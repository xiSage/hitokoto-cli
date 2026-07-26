using hitokoto_cli.Infrastructure;
using hitokoto_cli.Models;
using hitokoto_cli.Services;
using hitokoto_cli.Settings;
using Spectre.Console;
using Spectre.Console.Cli;

namespace hitokoto_cli.Commands;

/// <summary>Default command: fetch and print one sentence.</summary>
internal sealed class DefaultCommand(
    IHitokotoClient client,
    IConfigStore configStore,
    IAnsiConsole stdout,
    ErrorConsole stderr,
    OutputFormatter formatter)
{
    private readonly IHitokotoClient _client = client;
    private readonly IConfigStore _configStore = configStore;
    private readonly IAnsiConsole _stdout = stdout;
    private readonly ErrorConsole _stderr = stderr;
    private readonly OutputFormatter _formatter = formatter;

    public async Task<int> ExecuteAsync(CommandContext _, FetchSettings s, CancellationToken _1)
    {
        if (s.Format is not null && s.Raw is not null)
        {
            _stderr.Console.MarkupLine("[red]错误：--format 与 --raw 不能同时使用[/]");
            return 2;
        }

        EffectiveParams eff;
        if (s.NoConfig)
        {
            // Skip file creation/load entirely; use built-in defaults.
            eff = ConfigStore.Merge(s, AppConfig.Defaults);
        }
        else
        {
            _configStore.EnsureCreated();
            eff = ConfigStore.Merge(s, _configStore.Load());
        }

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(eff.TimeoutSeconds));

        if (s.Raw is { } raw)
        {
            var body = await _client.GetRawAsync(eff, raw, cts.Token);
            if (body is null)
            {
                return 1;
            }
            _stdout.WriteLine(body);
            return 0;
        }

        var resp = await _client.FetchAsync(eff, cts.Token);
        if (resp is null)
        {
            return 1;
        }
        OutputFormatter.Render(resp, eff.OutputFormat, eff.ShowSource, eff.ShowLink, _stdout);
        return 0;
    }
}
