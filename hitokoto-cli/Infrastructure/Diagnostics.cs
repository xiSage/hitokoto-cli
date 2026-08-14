using Spectre.Console;

namespace hitokoto_cli.Infrastructure;

/// <summary>
/// User-facing diagnostics: the sole owner of the exit-code protocol and the
/// stderr message formatting (escaping, colors, prefixes). Callers pass raw
/// text and get back the exit code; "错误：" / "警告：" prefixes, Markup.Escape
/// and the stdout/stderr split all live here. Replaces the pass-through
/// <c>ErrorConsole</c>.
/// </summary>
internal sealed class Diagnostics(IAnsiConsole console)
{
    // Exit-code protocol: 0 success, 1 runtime, 2 usage/config, 3 IO/unauthorized.
    public const int ExitUsage = 2;
    public const int ExitRuntime = 1;
    public const int ExitIo = 3;

    public int UsageError(string message) => ReportError(message, ExitUsage);

    public int RuntimeError(string message) => ReportError(message, ExitRuntime);

    public void Warn(string message)
        => console.MarkupLine($"[yellow]警告：{Markup.Escape(message)}[/]");

    public void Success(string label, string? detail = null)
    {
        var escapedLabel = Markup.Escape(label);
        console.MarkupLine(detail is null
            ? $"[green]{escapedLabel}[/]"
            : $"[green]{escapedLabel}[/] {Markup.Escape(detail)}");
    }

    public int HandleException(Exception ex)
        => ex is IOException or UnauthorizedAccessException
            ? ReportError(ex.Message, ExitIo)
            : ReportError(ex.Message, ExitRuntime);

    private int ReportError(string message, int exitCode)
    {
        console.MarkupLine($"[red]错误：{Markup.Escape(message)}[/]");
        return exitCode;
    }
}
