using System.Net;
using System.Text.Json;
using hitokoto_cli.Json;
using hitokoto_cli.Models;

namespace hitokoto_cli.Services;

/// <summary>
/// Fetch module: builds the URL (<see cref="HitokotoUrl"/>), performs the HTTP
/// call, and parses the response. Returns <see cref="FetchResult{T}"/> and has
/// no side effects — failure reporting belongs to the caller. The HttpClient is
/// constructor-injected so the seam stays real for tests.
/// </summary>
internal sealed class HitokotoClient(HttpClient httpClient) : IHitokotoClient
{
    public async Task<FetchResult<HitokotoResponse>> FetchAsync(EffectiveParams p, CancellationToken ct)
    {
        var url = HitokotoUrl.Build(p, encode: "json");
        var body = await SendAsync(url, ct);
        if (!body.IsSuccess)
        {
            return FetchResult<HitokotoResponse>.Fail(body.Failure!.Value, body.Message);
        }

        try
        {
            return FetchResult<HitokotoResponse>.Ok(
                JsonSerializer.Deserialize(body.Value!, HitokotoJsonContext.Default.HitokotoResponse)!);
        }
        catch (JsonException ex)
        {
            return FetchResult<HitokotoResponse>.Fail(FetchFailureKind.Parse, $"响应 JSON 解析失败：{ex.Message}");
        }
    }

    public async Task<FetchResult<string>> GetRawAsync(EffectiveParams p, RawEncode encode, CancellationToken ct)
    {
        var url = HitokotoUrl.Build(p, encode: encode == RawEncode.Json ? "json" : "text");
        return await SendAsync(url, ct);
    }

    private async Task<FetchResult<string>> SendAsync(string url, CancellationToken ct)
    {
        try
        {
            using var resp = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            if (resp.StatusCode != HttpStatusCode.OK)
            {
                return FetchResult<string>.Fail(FetchFailureKind.Http, $"HTTP {(int)resp.StatusCode} {resp.StatusCode}");
            }
            var body = await resp.Content.ReadAsStringAsync(ct);
            return FetchResult<string>.Ok(body);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return FetchResult<string>.Fail(FetchFailureKind.Timeout, "请求超时。");
        }
        catch (HttpRequestException ex)
        {
            return FetchResult<string>.Fail(FetchFailureKind.Network, $"网络请求失败：{ex.Message}");
        }
    }
}
