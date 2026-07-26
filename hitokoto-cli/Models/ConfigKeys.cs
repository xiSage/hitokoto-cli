namespace hitokoto_cli.Models;

internal enum ConfigKeyType
{
    String,
    Int,
    StringArray,
    Enum,
    Bool,
}

/// <summary>
/// Metadata for a single configuration key: how to parse a string value,
/// how to apply it to an <see cref="AppConfig"/>, how to read it back, and
/// how to clear it (set to null). Drives <c>config get/set/unset/list</c>.
/// </summary>
internal sealed record ConfigKeyInfo(
    string Key,
    ConfigKeyType Type,
    string ExpectedTypeDisplay,
    Action<AppConfig, object> Setter,
    Func<AppConfig, object?> Getter,
    Action<AppConfig> Clearer);

/// <summary>Registry of all valid configuration keys.</summary>
internal static class ConfigKeys
{
    public const string Endpoint = "endpoint";
    public const string Categories = "categories";
    public const string MinLength = "min_length";
    public const string MaxLength = "max_length";
    public const string OutputFormat = "output_format";
    public const string TimeoutSeconds = "timeout_seconds";
    public const string ShowSource = "show_source";
    public const string ShowLink = "show_link";

    public static readonly IReadOnlyDictionary<string, ConfigKeyInfo> All =
        new Dictionary<string, ConfigKeyInfo>(StringComparer.Ordinal)
        {
            [Endpoint] = new(Endpoint, ConfigKeyType.String, "字符串 (URL)",
                (c, v) => c.Endpoint = (string)v, c => c.Endpoint, c => c.Endpoint = null),
            [Categories] = new(Categories, ConfigKeyType.StringArray, "逗号分隔的分类列表 (a-l)",
                (c, v) => c.Categories = (string[])v, c => c.Categories, c => c.Categories = null),
            [MinLength] = new(MinLength, ConfigKeyType.Int, "整数",
                (c, v) => c.MinLength = (int)v, c => c.MinLength, c => c.MinLength = null),
            [MaxLength] = new(MaxLength, ConfigKeyType.Int, "整数",
                (c, v) => c.MaxLength = (int)v, c => c.MaxLength, c => c.MaxLength = null),
            [OutputFormat] = new(OutputFormat, ConfigKeyType.Enum, "text | json | full",
                (c, v) => c.OutputFormat = (OutputFormat)v, c => c.OutputFormat, c => c.OutputFormat = null),
            [TimeoutSeconds] = new(TimeoutSeconds, ConfigKeyType.Int, "整数 (秒)",
                (c, v) => c.TimeoutSeconds = (int)v, c => c.TimeoutSeconds, c => c.TimeoutSeconds = null),
            [ShowSource] = new(ShowSource, ConfigKeyType.Bool, "true | false",
                (c, v) => c.ShowSource = (bool)v, c => c.ShowSource, c => c.ShowSource = null),
            [ShowLink] = new(ShowLink, ConfigKeyType.Bool, "true | false",
                (c, v) => c.ShowLink = (bool)v, c => c.ShowLink, c => c.ShowLink = null),
        };

    public static bool TryGet(string key, out ConfigKeyInfo info)
        => All.TryGetValue(key, out info!);

    /// <summary>Renders a config value for display (list/get).</summary>
    public static string FormatValue(object? value)
    {
        return value switch
        {
            null => string.Empty,
            string[] arr => string.Join(", ", arr),
            bool b => b ? "true" : "false",
            Enum e => e.ToString().ToLowerInvariant(),
            _ => value.ToString() ?? string.Empty,
        };
    }
}
