using hitokoto_cli.Models;

namespace hitokoto_cli.Services;

internal interface IHitokotoClient
{
    /// <summary>Fetches a sentence and parses it.</summary>
    Task<FetchResult<HitokotoResponse>> FetchAsync(EffectiveParams p, CancellationToken ct);

    /// <summary>Fetches the raw response body for the given encode.</summary>
    Task<FetchResult<string>> GetRawAsync(EffectiveParams p, RawEncode encode, CancellationToken ct);
}
