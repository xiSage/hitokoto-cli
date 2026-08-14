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
    ConfigModule config,
    IAnsiConsole stdout,
    Diagnostics diagnostics)
{
    private readonly IHitokotoClient _client = client;
    private readonly ConfigModule _config = config;
    private readonly IAnsiConsole _stdout = stdout;
    private readonly Diagnostics _diagnostics = diagnostics;

    public async Task<int> ExecuteAsync(CommandContext _, FetchSettings s, CancellationToken _1)
    {
        if (s.Format is not null && s.Raw is not null)
        {
            return _diagnostics.UsageError("--format 与 --raw 不能同时使用");
        }

        var overrides = new CliOverrides(
            Category: s.Category,
            MinLength: s.MinLength,
            MaxLength: s.MaxLength,
            Endpoint: s.Endpoint,
            Format: s.Format,
            ShowSource: s.ShowSource,
            ShowLink: s.ShowLink);

        var eff = _config.Resolve(overrides, useFileConfig: !s.NoConfig);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(eff.TimeoutSeconds));

        if (s.Raw is { } raw)
        {
            var result = await _client.GetRawAsync(eff, raw, cts.Token);
            if (!result.IsSuccess)
            {
                _diagnostics.RuntimeError(result.Message);
                return Diagnostics.ExitRuntime;
            }
            _stdout.WriteLine(result.Value!);
            return 0;
        }

        var resp = await _client.FetchAsync(eff, cts.Token);
        if (!resp.IsSuccess)
        {
            _diagnostics.RuntimeError(resp.Message);
            return Diagnostics.ExitRuntime;
        }
        OutputFormatter.Render(resp.Value!, eff.OutputFormat, eff.ShowSource, eff.ShowLink, _stdout);
        return 0;
    }
}
