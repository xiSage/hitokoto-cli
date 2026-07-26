using Spectre.Console;

namespace hitokoto_cli.Infrastructure;

/// <summary>
/// Wrapper around an <see cref="IAnsiConsole"/> pointed at stderr, used for
/// prompts/warnings/errors so that stdout stays clean for piped output
/// (<c>--format json</c>, <c>--raw</c>). Distinct type from
/// <see cref="IAnsiConsole"/> so DI can resolve both independently.
/// </summary>
internal sealed class ErrorConsole(IAnsiConsole console)
{
    public IAnsiConsole Console { get; } = console;
}
