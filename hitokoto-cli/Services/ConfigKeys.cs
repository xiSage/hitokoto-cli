using System.Globalization;
using hitokoto_cli.Models;

namespace hitokoto_cli.Services;

internal enum ConfigKeyType
{
    String,
    Int,
    StringArray,
    Enum,
    Bool,
}

/// <summary>
/// Metadata for a single configuration key: how to parse a string value, the
/// merge policy (CLI override slot, built-in fallback, API-facing omission),
/// and how to apply/read/clear it on an <see cref="AppConfig"/>. One row
/// knows everything about one key — drives <c>config get/set/unset/list</c>
/// and <see cref="ConfigModule.Resolve"/>. Adding a key = one row here plus
/// its <see cref="AppConfig"/> property and its CLI option.
/// </summary>
internal sealed record ConfigKeyInfo(
    string Key,
    ConfigKeyType Type,
    string ExpectedTypeDisplay,
    bool OmitIfUnset,
    object? DefaultValue,
    Func<CliOverrides, object?>? Override,
    Action<AppConfig, object> Setter,
    Func<AppConfig, object?> Getter,
    Action<AppConfig> Clearer)
{
    /// <summary>
    /// Parses a CLI-provided string into the key's typed value.
    /// <c>false</c> means the value cannot be represented (exit code 2).
    /// </summary>
    public bool TryParse(string raw, out object? value)
    {
        try
        {
            value = Type switch
            {
                ConfigKeyType.String => raw,
                ConfigKeyType.Int => int.Parse(raw, CultureInfo.InvariantCulture),
                ConfigKeyType.Enum => Enum.Parse<OutputFormat>(raw, ignoreCase: true),
                ConfigKeyType.Bool => bool.Parse(raw),
                ConfigKeyType.StringArray => raw.Split(',',
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                _ => throw new InvalidOperationException("未知键类型"),
            };
            return true;
        }
        catch
        {
            value = null;
            return false;
        }
    }
}

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
            // Client-facing: falls back to the built-in endpoint when unset everywhere.
            [Endpoint] = new(Endpoint, ConfigKeyType.String, "字符串 (URL)",
                OmitIfUnset: false, DefaultValue: AppConfig.Defaults.Endpoint,
                Override: o => o.Endpoint,
                (c, v) => c.Endpoint = (string)v, c => c.Endpoint, c => c.Endpoint = null),
            // API-facing: null at every level means "omit the param, let the API choose".
            [Categories] = new(Categories, ConfigKeyType.StringArray, "逗号分隔的分类列表 (a-l)",
                OmitIfUnset: true, DefaultValue: null,
                Override: o => o.Category is { Length: > 0 } ? o.Category : null,
                (c, v) => c.Categories = (string[])v, c => c.Categories, c => c.Categories = null),
            [MinLength] = new(MinLength, ConfigKeyType.Int, "整数",
                OmitIfUnset: true, DefaultValue: null,
                Override: o => o.MinLength,
                (c, v) => c.MinLength = (int)v, c => c.MinLength, c => c.MinLength = null),
            [MaxLength] = new(MaxLength, ConfigKeyType.Int, "整数",
                OmitIfUnset: true, DefaultValue: null,
                Override: o => o.MaxLength,
                (c, v) => c.MaxLength = (int)v, c => c.MaxLength, c => c.MaxLength = null),
            // Client-facing: falls back to the built-in format (full) when unset.
            [OutputFormat] = new(OutputFormat, ConfigKeyType.Enum, "text | json | full",
                OmitIfUnset: false, DefaultValue: AppConfig.Defaults.OutputFormat,
                Override: o => o.Format,
                (c, v) => c.OutputFormat = (OutputFormat)v, c => c.OutputFormat, c => c.OutputFormat = null),
            // Client-facing, no CLI slot: only file value or the built-in timeout.
            [TimeoutSeconds] = new(TimeoutSeconds, ConfigKeyType.Int, "整数 (秒)",
                OmitIfUnset: false, DefaultValue: AppConfig.Defaults.TimeoutSeconds,
                Override: null,
                (c, v) => c.TimeoutSeconds = (int)v, c => c.TimeoutSeconds, c => c.TimeoutSeconds = null),
            // Client-facing rendering toggles. Merge default is true, but the
            // file default stays null ("unset") so `config list` shows 未设置.
            [ShowSource] = new(ShowSource, ConfigKeyType.Bool, "true | false",
                OmitIfUnset: false, DefaultValue: true,
                Override: o => o.ShowSource,
                (c, v) => c.ShowSource = (bool)v, c => c.ShowSource, c => c.ShowSource = null),
            [ShowLink] = new(ShowLink, ConfigKeyType.Bool, "true | false",
                OmitIfUnset: false, DefaultValue: true,
                Override: o => o.ShowLink,
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
