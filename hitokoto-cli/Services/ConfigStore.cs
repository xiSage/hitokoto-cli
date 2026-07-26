using System.Text.Json;
using hitokoto_cli.Infrastructure;
using hitokoto_cli.Json;
using hitokoto_cli.Models;
using hitokoto_cli.Settings;
using Spectre.Console;

namespace hitokoto_cli.Services;

internal sealed class ConfigStore(ErrorConsole stderr) : IConfigStore
{
    private readonly ErrorConsole _stderr = stderr; // messages go to stderr to keep stdout clean

    public static string GetFilePath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "hitokoto-cli", "config.json");
    }

    public bool FileExists => File.Exists(GetFilePath());

    public void EnsureCreated()
    {
        var path = GetFilePath();
        if (File.Exists(path))
        {
            return;
        }

        EnsureDirectory(path);
        SaveInternal(AppConfig.Defaults);
        _stderr.Console.MarkupLine($"[green]已创建默认配置文件：[/]{Markup.Escape(path)}");
    }

    public AppConfig Load()
    {
        var path = GetFilePath();
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
            _stderr.Console.MarkupLine($"[yellow]警告：配置文件解析失败（{Markup.Escape(ex.Message)}），使用默认值。[/]");
            return AppConfig.Defaults;
        }
        catch (IOException ex)
        {
            _stderr.Console.MarkupLine($"[yellow]警告：配置文件读取失败（{Markup.Escape(ex.Message)}），使用默认值。[/]");
            return AppConfig.Defaults;
        }
    }

    public void Save(AppConfig config)
    {
        var path = GetFilePath();
        EnsureDirectory(path);
        SaveInternal(config);
    }

    public void Reset()
    {
        var path = GetFilePath();
        EnsureDirectory(path);
        SaveInternal(AppConfig.Defaults);
        _stderr.Console.MarkupLine($"[green]已重置配置文件：[/]{Markup.Escape(path)}");
    }

    /// <summary>
    /// Merges CLI overrides over loaded config. API-facing params
    /// (Categories, MinLength, MaxLength) have no built-in fallback: a null
    /// at every level means "omit the param, let the API choose". Client-side
    /// params (Endpoint, OutputFormat, TimeoutSeconds) fall back to
    /// <see cref="AppConfig.Defaults"/> when unset everywhere.
    /// <paramref name="settings"/> members that are null/empty are treated as
    /// "not specified on CLI".
    /// </summary>
    public static EffectiveParams Merge(FetchSettings settings, AppConfig config)
    {
        var defaults = AppConfig.Defaults;

        return new EffectiveParams(
            Endpoint: settings.Endpoint ?? config.Endpoint ?? defaults.Endpoint!,
            // CLI override wins; else config value (may be null = "let API
            // choose"). No built-in fallback — null propagates to the URL.
            Categories: settings.Category is { Length: > 0 } ? settings.Category : config.Categories,
            MinLength: settings.MinLength ?? config.MinLength,
            MaxLength: settings.MaxLength ?? config.MaxLength,
            TimeoutSeconds: config.TimeoutSeconds ?? defaults.TimeoutSeconds!.Value,
            OutputFormat: settings.Format ?? config.OutputFormat ?? defaults.OutputFormat!.Value,
            // Full-format rendering toggles: default to true (show) when unset
            // everywhere. These are client-side preferences, not API params.
            ShowSource: settings.ShowSource ?? config.ShowSource ?? true,
            ShowLink: settings.ShowLink ?? config.ShowLink ?? true);
    }

    private static void EnsureDirectory(string path)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }

    private static void SaveInternal(AppConfig config)
    {
        var path = GetFilePath();
        // Use Shared (not Default) so the relaxed JavaScriptEncoder emits
        // Chinese verbatim instead of \uXXXX escapes when writing the file.
        var json = JsonSerializer.Serialize(config, HitokotoJsonContext.Shared.AppConfig);

        // Atomic write: temp file in the same directory, then rename-over.
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, json);
        File.Move(tmp, path, overwrite: true);
    }
}
