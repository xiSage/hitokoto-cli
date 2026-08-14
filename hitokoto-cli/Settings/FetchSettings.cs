using System.ComponentModel;
using hitokoto_cli.Infrastructure;
using hitokoto_cli.Models;
using Spectre.Console.Cli;

namespace hitokoto_cli.Settings;

/// <summary>Options for the default (fetch) command. All nullable so that
/// "not specified on CLI" is distinguishable from "explicitly set".
/// Option specs and descriptions come from <see cref="CliSurface.Options"/>
/// so help text and the CLI binding share a single source.</summary>
internal sealed class FetchSettings : CommandSettings
{
    [CommandOption(CliSurface.Options.CategorySpec)]
    [Description(CliSurface.Options.CategoryDesc)]
    public string[]? Category { get; set; }

    [CommandOption(CliSurface.Options.MinLengthSpec)]
    [Description(CliSurface.Options.MinLengthDesc)]
    public int? MinLength { get; set; }

    [CommandOption(CliSurface.Options.MaxLengthSpec)]
    [Description(CliSurface.Options.MaxLengthDesc)]
    public int? MaxLength { get; set; }

    [CommandOption(CliSurface.Options.EndpointSpec)]
    [Description(CliSurface.Options.EndpointDesc)]
    public string? Endpoint { get; set; }

    [CommandOption(CliSurface.Options.FormatSpec)]
    [Description(CliSurface.Options.FormatDesc)]
    public OutputFormat? Format { get; set; }

    [CommandOption(CliSurface.Options.RawSpec)]
    [Description(CliSurface.Options.RawDesc)]
    public RawEncode? Raw { get; set; }

    [CommandOption(CliSurface.Options.ShowSourceSpec)]
    [Description(CliSurface.Options.ShowSourceDesc)]
    public bool? ShowSource { get; set; }

    [CommandOption(CliSurface.Options.ShowLinkSpec)]
    [Description(CliSurface.Options.ShowLinkDesc)]
    public bool? ShowLink { get; set; }

    [CommandOption(CliSurface.Options.NoConfigSpec)]
    [Description(CliSurface.Options.NoConfigDesc)]
    public bool NoConfig { get; set; }
}
