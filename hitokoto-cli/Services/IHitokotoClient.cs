using hitokoto_cli.Models;

namespace hitokoto_cli.Services;

internal interface IHitokotoClient
{
    /// <summary>Fetches a sentence and parses it. Returns null on error.</summary>
    Task<HitokotoResponse?> FetchAsync(EffectiveParams p, CancellationToken ct);

    /// <summary>Fetches the raw response body for the given encode. Returns null on error.</summary>
    Task<string?> GetRawAsync(EffectiveParams p, RawEncode encode, CancellationToken ct);
}
