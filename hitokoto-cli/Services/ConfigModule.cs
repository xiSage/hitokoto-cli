using System.Text.Json;
using hitokoto_cli.Infrastructure;
using hitokoto_cli.Json;
using hitokoto_cli.Models;

namespace hitokoto_cli.Services;

/// <summary>
/// Deep module owning everything about the configuration: the key table
/// (parse, merge precedence, defaults), file persistence, and the resolution
/// of effective fetch parameters. Commands and <c>DefaultCommand</c> cross
/// this one seam; the key table in <see cref="ConfigKeys"/> is an internal
/// seam used only by this module. The config file path is constructor-injected
/// so persistence is testable against a temp directory through the real seam.
/// </summary>
internal sealed class ConfigModule(string path, Diagnostics diagnostics)
{
    private readonly Diagnostics _diagnostics = diagnostics; // messages go to stderr to keep stdout clean

    /// <summary>The config file path this module reads and writes.</summary>
    public string Path => path;

    public bool FileExists => File.Exists(path);

    public static string GetDefaultFilePath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return System.IO.Path.Combine(appData, "hitokoto-cli", "config.json");
    }

    public void EnsureCreated()
    {
        if (File.Exists(path))
        {
            return;
        }

        EnsureDirectory(path);
        SaveInternal(AppConfig.Defaults);
        _diagnostics.Success("已创建默认配置文件：", path);
    }

    public AppConfig Load()
    {
        if (!File.Exists(path))
        {
            return AppConfig.Defaults;
        }

        try
        {
            var json = File.ReadAllText(path);
            var cfg = JsonSerializer.Deserialize(json, HitokotoJsonContext.Default.AppConfig);
            return cfg ?? AppConfig.Defaults;
        }
        catch (JsonException ex)
        {
            _diagnostics.Warn($"配置文件解析失败（{ex.Message}），使用默认值。");
            return AppConfig.Defaults;
        }
        catch (IOException ex)
        {
            _diagnostics.Warn($"配置文件读取失败（{ex.Message}），使用默认值。");
            return AppConfig.Defaults;
        }
    }

    public void Save(AppConfig config)
    {
        EnsureDirectory(path);
        SaveInternal(config);
    }

    public void Reset()
    {
        EnsureDirectory(path);
        SaveInternal(AppConfig.Defaults);
        _diagnostics.Success("已重置配置文件：", path);
    }

    /// <summary>
    /// Resolves the effective fetch parameters by merging CLI overrides over
    /// file configuration (or built-in defaults when
    /// <paramref name="useFileConfig"/> is false, i.e. --no-config). The merge
    /// is table-driven: each key's policy (override slot, precedence, built-in
    /// fallback, API-facing omission) lives in <see cref="ConfigKeys.All"/>.
    /// </summary>
    public EffectiveParams Resolve(CliOverrides overrides, bool useFileConfig)
    {
        var config = AppConfig.Defaults;
        if (useFileConfig)
        {
            EnsureCreated();
            config = Load();
        }

        var resolved = new Dictionary<string, object?>();
        foreach (var (key, info) in ConfigKeys.All)
        {
            // Uniform merge rule: CLI override wins, then file config, then the
            // built-in fallback (null when the param is API-facing and is simply
            // omitted from the request, letting the API choose).
            resolved[key] = info.Override?.Invoke(overrides)
                ?? info.Getter(config)
                ?? (info.OmitIfUnset ? null : info.DefaultValue);
        }

        return new EffectiveParams(
            Endpoint: (string)resolved[ConfigKeys.Endpoint]!,
            Categories: (IReadOnlyList<string>?)resolved[ConfigKeys.Categories],
            MinLength: (int?)resolved[ConfigKeys.MinLength],
            MaxLength: (int?)resolved[ConfigKeys.MaxLength],
            TimeoutSeconds: (int)resolved[ConfigKeys.TimeoutSeconds]!,
            OutputFormat: (OutputFormat)resolved[ConfigKeys.OutputFormat]!,
            ShowSource: (bool)resolved[ConfigKeys.ShowSource]!,
            ShowLink: (bool)resolved[ConfigKeys.ShowLink]!);
    }

    public static bool TryGetKey(string key, out ConfigKeyInfo info)
        => ConfigKeys.TryGet(key, out info!);

    public static IReadOnlyDictionary<string, ConfigKeyInfo> Keys => ConfigKeys.All;

    public static string FormatValue(object? value) => ConfigKeys.FormatValue(value);

    private static void EnsureDirectory(string path)
    {
        var dir = System.IO.Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }

    private void SaveInternal(AppConfig config)
    {
        // Use Shared (not Default) so the relaxed JavaScriptEncoder emits
        // Chinese verbatim instead of \uXXXX escapes when writing the file.
        var json = JsonSerializer.Serialize(config, HitokotoJsonContext.Shared.AppConfig);

        // Atomic write: temp file in the same directory, then rename-over.
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, json);
        File.Move(tmp, path, overwrite: true);
    }
}
