using hitokoto_cli.Models;

namespace hitokoto_cli.Services;

/// <summary>
/// Fully-resolved request parameters after merging CLI overrides over
/// configuration defaults.
///
/// API-facing members (<see cref="Categories"/>, <see cref="MinLength"/>,
/// <see cref="MaxLength"/>) are nullable: <c>null</c> means "don't send the
/// param, let the API choose". Client-facing members (<see cref="Endpoint"/>,
/// <see cref="TimeoutSeconds"/>, <see cref="OutputFormat"/>,
/// <see cref="ShowSource"/>, <see cref="ShowLink"/>) are always decided —
/// they have no "let the API choose" semantics. <see cref="ShowSource"/> and
/// <see cref="ShowLink"/> only affect <see cref="OutputFormat.Full"/> output.
/// </summary>
internal sealed record EffectiveParams(
    string Endpoint,
    IReadOnlyList<string>? Categories,
    int? MinLength,
    int? MaxLength,
    int TimeoutSeconds,
    OutputFormat OutputFormat,
    bool ShowSource,
    bool ShowLink);
