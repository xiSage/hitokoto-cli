using System.Globalization;

namespace hitokoto_cli.Services;

/// <summary>
/// Builds the hitokoto API URL from effective fetch parameters. The omit-null
/// protocol lives here: API-facing params (categories/min_length/max_length)
/// are sent only when set — null means "let the API choose" and the param is
/// omitted entirely. <c>encode</c> is always sent: it's a protocol detail, not
/// a user preference.
/// </summary>
internal static class HitokotoUrl
{
    public static string Build(EffectiveParams p, string encode)
    {
        var baseUri = p.Endpoint.TrimEnd('/');
        var query = new List<string>();

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

        query.Add($"encode={encode}");

        return $"{baseUri}/?{string.Join('&', query)}";
    }
}
