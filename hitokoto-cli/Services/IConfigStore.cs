using hitokoto_cli.Models;

namespace hitokoto_cli.Services;

internal interface IConfigStore
{
    /// <summary>Whether the config file currently exists on disk.</summary>
    bool FileExists { get; }

    /// <summary>Creates the config file with defaults if it does not exist.</summary>
    void EnsureCreated();

    /// <summary>Loads the config, falling back to defaults if absent or corrupt.</summary>
    AppConfig Load();

    /// <summary>Atomically writes the config to disk.</summary>
    void Save(AppConfig config);

    /// <summary>Overwrites the config file with built-in defaults.</summary>
    void Reset();
}
