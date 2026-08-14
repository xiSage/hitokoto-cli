using hitokoto_cli.Models;

namespace hitokoto_cli.Services;

/// <summary>
/// Fetch options as given on the command line. All members are nullable:
/// null/empty means "not specified on the CLI". Merged over file config by
/// <see cref="ConfigModule.Resolve"/>; never depends on Spectre settings types,
/// so tests (or any future caller) can build one without a settings object.
/// </summary>
internal sealed record CliOverrides(
    string[]? Category,
    int? MinLength,
    int? MaxLength,
    string? Endpoint,
    OutputFormat? Format,
    bool? ShowSource,
    bool? ShowLink);
