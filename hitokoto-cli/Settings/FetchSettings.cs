using System.ComponentModel;
using hitokoto_cli.Models;
using Spectre.Console.Cli;

namespace hitokoto_cli.Settings;

/// <summary>Options for the default (fetch) command. All nullable so that
/// "not specified on CLI" is distinguishable from "explicitly set".</summary>
internal sealed class FetchSettings : CommandSettings
{
    [CommandOption("-c|--category <CATEGORY>")]
    [Description("句子分类 (a-l)，可多次指定；缺省则不限制")]
    public string[]? Category { get; set; }

    [CommandOption("--min-length <N>")]
    [Description("句子最小长度 (含)；缺省则不限制")]
    public int? MinLength { get; set; }

    [CommandOption("--max-length <N>")]
    [Description("句子最大长度 (含)；缺省则不限制")]
    public int? MaxLength { get; set; }

    [CommandOption("--endpoint <URL>")]
    [Description("API 端点 URL")]
    public string? Endpoint { get; set; }

    [CommandOption("--format <FORMAT>")]
    [Description("CLI 输出格式: text | json | full")]
    public OutputFormat? Format { get; set; }

    [CommandOption("--raw <ENCODE>")]
    [Description("透传 API encode 原样输出: text | json (与 --format 互斥)")]
    public RawEncode? Raw { get; set; }

    [CommandOption("--show-source <TRUE_FALSE>")]
    [Description("full 格式是否显示来源 (true|false，缺省则显示)")]
    public bool? ShowSource { get; set; }

    [CommandOption("--show-link <TRUE_FALSE>")]
    [Description("full 格式是否显示链接 (true|false，缺省则显示)")]
    public bool? ShowLink { get; set; }

    [CommandOption("--no-config")]
    [Description("忽略配置文件，使用内置默认值")]
    public bool NoConfig { get; set; }
}
