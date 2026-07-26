using Spectre.Console;

namespace hitokoto_cli;

/// <summary>
/// Prints the top-level help for the hitokoto CLI. Replaces Spectre.Console.Cli's
/// auto-generated help (which would expose the hidden <c>__default__</c> command
/// name and omit the default-command options from the root help) with a single
/// comprehensive view covering both the fetch options and the config subcommands.
/// </summary>
internal static class HelpPrinter
{
    public static void Print(IAnsiConsole console)
    {
        console.WriteLine();
        console.MarkupLine("[bold yellow]hitokoto[/] — 一言 (Hitokoto) CLI 工具");
        console.MarkupLine("[dim]从 https://hitokoto.cn 获取一句话并输出。[/]");
        console.WriteLine();

        console.MarkupLine("[bold]用法[/]");
        console.MarkupLine("  [green]hitokoto[/] [grey][[OPTIONS]][/]");
        console.MarkupLine("  [green]hitokoto config[/] [grey]<COMMAND>[/]");
        console.MarkupLine("  [green]hitokoto[/] [grey][[--help|-h]][/]");
        console.MarkupLine("  [green]hitokoto[/] [grey][[--version|-v]][/]");
        console.WriteLine();

        console.MarkupLine("[bold]选项（默认获取一言）[/]");
        PrintOption(console, "-c, --category <CATEGORY>", "句子分类 (a-l)，可多次指定；缺省则不限制");
        PrintOption(console, "    --min-length <N>", "句子最小长度（含）；缺省则不限制");
        PrintOption(console, "    --max-length <N>", "句子最大长度（含）；缺省则不限制");
        PrintOption(console, "    --endpoint <URL>", "API 端点 URL");
        PrintOption(console, "    --format <FORMAT>", "CLI 输出格式: text | json | full（默认 full）");
        PrintOption(console, "    --raw <ENCODE>", "透传 API 响应: text | json（与 --format 互斥）");
        PrintOption(console, "    --show-source <TRUE_FALSE>", "full 格式是否显示来源（默认 true）");
        PrintOption(console, "    --show-link <TRUE_FALSE>", "full 格式是否显示链接（默认 true）");
        PrintOption(console, "    --no-config", "忽略配置文件，使用内置默认值");
        PrintOption(console, "-h, --help", "显示此帮助信息");
        PrintOption(console, "-v, --version", "显示版本信息");
        console.WriteLine();

        console.MarkupLine("[bold]分类代码 (a-l)[/]");
        var legend = new Grid();
        for (int i = 0; i < 4; i++)
        {
            legend.AddColumn(new GridColumn().PadRight(4));
        }
        legend.AddRow("[grey]a[/] 动画", "[grey]b[/] 漫画", "[grey]c[/] 游戏", "[grey]d[/] 文学");
        legend.AddRow("[grey]e[/] 原创", "[grey]f[/] 来自网络", "[grey]g[/] 其他", "[grey]h[/] 影视");
        legend.AddRow("[grey]i[/] 诗词", "[grey]j[/] 网易云", "[grey]k[/] 哲学", "[grey]l[/] 抖机灵");
        console.Write(legend);
        console.WriteLine();

        console.MarkupLine("[bold]子命令[/]");
        console.MarkupLine("  [green]config[/]    查询或修改配置文件");
        console.WriteLine();

        console.MarkupLine("[bold]config 子命令[/]");
        PrintConfigCommand(console, "list", "列出全部配置值");
        PrintConfigCommand(console, "get <key>", "获取配置值");
        PrintConfigCommand(console, "set <key> <value>", "设置配置值（校验类型）");
        PrintConfigCommand(console, "unset <key>", "清除配置值（恢复默认）");
        PrintConfigCommand(console, "path", "显示配置文件路径");
        PrintConfigCommand(console, "reset", "重置配置文件为默认值");
        console.WriteLine();

        console.MarkupLine("[bold]示例[/]");
        console.MarkupLine("  [green]hitokoto[/]                                  获取一言（默认 full 格式）");
        console.MarkupLine("  [green]hitokoto[/] -c a -c c                        多分类覆盖");
        console.MarkupLine("  [green]hitokoto[/] --min-length 10 --max-length 50  限定长度");
        console.MarkupLine("  [green]hitokoto[/] --format json                    JSON 格式输出");
        console.MarkupLine("  [green]hitokoto[/] --raw text                       透传 API 文本响应");
        console.MarkupLine("  [green]hitokoto[/] --no-config                      忽略配置文件");
        console.MarkupLine("  [green]hitokoto[/] --show-source false              full 仅显示句子");
        console.MarkupLine("  [green]hitokoto[/] --show-link false                full 不显示链接");
        console.MarkupLine("  [green]hitokoto[/] config set max_length 100        设置配置值");
        console.WriteLine();

        console.MarkupLine("[bold]配置文件[/]");
        console.MarkupLine("  路径由 [green]hitokoto config path[/] 显示。首次运行自动创建默认配置。");
        console.MarkupLine("  参数优先级: [yellow]CLI 参数 > 配置文件 > 缺省（API 参数不发送，由 API 选择）[/]");
        console.WriteLine();
    }

    private static void PrintOption(IAnsiConsole console, string spec, string desc)
    {
        // Escape spec/desc for markup; pad spec so descriptions align. For
        // wide spec strings, place the description on the next line.
        const int col = 30;
        var escapedSpec = Markup.Escape(spec);
        var padded = spec.Length >= col
            ? escapedSpec + "\n" + new string(' ', col)
            : escapedSpec.PadRight(col);
        console.MarkupLine($"  [cyan]{padded}[/]{Markup.Escape(desc)}");
    }

    private static void PrintConfigCommand(IAnsiConsole console, string spec, string desc)
    {
        const int col = 26;
        var escapedSpec = Markup.Escape(spec);
        var padded = spec.Length >= col
            ? escapedSpec + "\n" + new string(' ', col)
            : escapedSpec.PadRight(col);
        console.MarkupLine($"  [green]config[/] [cyan]{padded}[/]{Markup.Escape(desc)}");
    }
}
