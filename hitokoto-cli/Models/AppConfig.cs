namespace hitokoto_cli.Models;

/// <summary>
/// On-disk configuration schema. All properties are nullable so that
/// "unset" can be represented by omission; missing values fall back to
/// <see cref="Defaults"/> at merge time. Property naming is handled by
/// the JSON source generator (snake_case).
/// </summary>
internal sealed class AppConfig
{
    public const string DefaultEndpoint = "https://v1.hitokoto.cn";

    public string? Endpoint { get; set; }
    public string[]? Categories { get; set; }
    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    public OutputFormat? OutputFormat { get; set; }
    public int? TimeoutSeconds { get; set; }
    public bool? ShowSource { get; set; }
    public bool? ShowLink { get; set; }

    /// <summary>
    /// Built-in defaults used when no value is present anywhere. API-facing
    /// params (Categories, MinLength, MaxLength) default to <c>null</c>: the
    /// param is omitted from the request and the API picks its own behavior.
    /// Only client-side concerns (Endpoint, OutputFormat, TimeoutSeconds)
    /// carry built-in defaults.
    /// </summary>
    public static AppConfig Defaults => new()
    {
        Endpoint = DefaultEndpoint,
        // null = omit from request, let the API choose.
        Categories = null,
        MinLength = null,
        MaxLength = null,
        // Property name shadows the enum type; fully qualify to disambiguate.
        OutputFormat = global::hitokoto_cli.Models.OutputFormat.Full,
        TimeoutSeconds = 5,
        // null = omit from config file; EffectiveParams falls back to true
        // (show) at merge time. These are client-side rendering preferences,
        // not API params, so a concrete default is appropriate.
        ShowSource = null,
        ShowLink = null,
    };
}
