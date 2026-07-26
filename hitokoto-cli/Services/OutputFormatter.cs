using System.Text;
using System.Text.Json;
using hitokoto_cli.Json;
using hitokoto_cli.Models;
using Spectre.Console;

namespace hitokoto_cli.Services;

internal sealed class OutputFormatter
{
    public static void Render(HitokotoResponse resp, OutputFormat fmt, bool showSource, bool showLink, IAnsiConsole console)
    {
        switch (fmt)
        {
            case OutputFormat.Text:
                console.WriteLine(resp.Hitokoto ?? string.Empty);
                break;

            case OutputFormat.Json:
                // Use Shared (not Default) so the relaxed JavaScriptEncoder emits
                // Chinese verbatim instead of \uXXXX escapes in --format json output.
                var json = JsonSerializer.Serialize(resp, HitokotoJsonContext.Shared.HitokotoResponse);
                console.WriteLine(json);
                break;

            case OutputFormat.Full:
                console.Write(BuildFullPanel(resp, showSource, showLink));
                break;
        }
    }

    private static Panel BuildFullPanel(HitokotoResponse resp, bool showSource, bool showLink)
    {
        var markup = new StringBuilder();

        markup.Append("[white]").Append(Markup.Escape(resp.Hitokoto ?? string.Empty)).Append("[/]");

        if (showSource)
        {
            var hasFrom = !string.IsNullOrWhiteSpace(resp.From);
            var hasWho = !string.IsNullOrWhiteSpace(resp.FromWho);
            if (hasFrom || hasWho)
            {
                markup.Append("\n[dim]");
                if (hasWho)
                {
                    markup.Append(Markup.Escape(resp.FromWho!));
                }
                if (hasFrom)
                {
                    if (hasWho)
                    {
                        markup.Append(' ');
                    }
                    markup.Append('《').Append(Markup.Escape(resp.From!)).Append('》');
                }
                markup.Append("[/]");
            }
        }

        if (showLink && !string.IsNullOrWhiteSpace(resp.Uuid))
        {
            markup.Append("\n[blue]https://hitokoto.cn?uuid=")
                  .Append(Markup.Escape(resp.Uuid))
                  .Append("[/]");
        }

        return new Panel(new Markup(markup.ToString()))
        {
            Border = BoxBorder.Rounded,
            Padding = new Padding(2, 0, 2, 0),
        };
    }
}
