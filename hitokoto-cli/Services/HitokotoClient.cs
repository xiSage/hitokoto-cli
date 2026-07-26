using System.Globalization;
using System.Net;
using System.Text.Json;
using hitokoto_cli.Infrastructure;
using hitokoto_cli.Json;
using hitokoto_cli.Models;
using Spectre.Console;

namespace hitokoto_cli.Services;

internal sealed class HitokotoClient(ErrorConsole stderr) : IHitokotoClient
{
    private static readonly HttpClient HttpClient = new()
    {
        // Per-request timeout is enforced via the caller's CancellationToken;
        // keep the HTTP-level timeout slightly above any reasonable config.
        Timeout = TimeSpan.FromSeconds(30),
    };

    private readonly ErrorConsole _stderr = stderr;

    public async Task<HitokotoResponse?> FetchAsync(EffectiveParams p, CancellationToken ct)
    {
        var url = BuildUrl(p, encode: "json");
        var body = await SendAsync(url, ct);
        if (body is null)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize(body, HitokotoJsonContext.Default.HitokotoResponse);
        }
        catch (JsonException ex)
        {
            _stderr.Console.MarkupLine($"[red]错误：响应 JSON 解析失败：{Markup.Escape(ex.Message)}[/]");
            return null;
        }
    }

    public async Task<string?> GetRawAsync(EffectiveParams p, RawEncode encode, CancellationToken ct)
    {
        var url = BuildUrl(p, encode: encode == RawEncode.Json ? "json" : "text");
        return await SendAsync(url, ct);
    }

    private async Task<string?> SendAsync(string url, CancellationToken ct)
    {
        try
        {
            using var resp = await HttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            if (resp.StatusCode != HttpStatusCode.OK)
            {
                _stderr.Console.MarkupLine($"[red]错误：HTTP {(int)resp.StatusCode} {Markup.Escape(resp.StatusCode.ToString())}[/]");
                return null;
            }
            return await resp.Content.ReadAsStringAsync(ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _stderr.Console.MarkupLine("[red]错误：请求超时。[/]");
            return null;
        }
        catch (HttpRequestException ex)
        {
            _stderr.Console.MarkupLine($"[red]错误：网络请求失败：{Markup.Escape(ex.Message)}[/]");
            return null;
        }
    }

    private static string BuildUrl(EffectiveParams p, string encode)
    {
        var baseUri = p.Endpoint.TrimEnd('/');
        var query = new List<string>();

        // Each API param is sent only when the caller expressed a preference;
        // null means "let the API choose" and is omitted entirely.
        if (p.Categories is { Count: > 0 } cats)
        {
            foreach (var c in cats)
            {
                query.Add($"c={Uri.EscapeDataString(c)}");
            }
        }

        if (p.MinLength is { } min)
        {
            query.Add($"min_length={min.ToString(CultureInfo.InvariantCulture)}");
        }

        if (p.MaxLength is { } max)
        {
            query.Add($"max_length={max.ToString(CultureInfo.InvariantCulture)}");
        }

        // encode is always sent: the client requires a specific response shape
        // (json for parsing, or text for --raw text). It's a protocol detail,
        // not a user preference, so it doesn't follow the "omit if unspecified" rule.
        query.Add($"encode={encode}");

        return $"{baseUri}/?{string.Join('&', query)}";
    }
}
