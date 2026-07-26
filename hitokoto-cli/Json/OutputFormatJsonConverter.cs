using System.Text.Json;
using System.Text.Json.Serialization;
using hitokoto_cli.Models;

namespace hitokoto_cli.Json;

/// <summary>
/// AOT-safe <see cref="JsonConverter{T}"/> for <see cref="OutputFormat"/> that
/// reads case-insensitively and writes lowercase strings ("text"/"json"/"full"),
/// matching the config-file schema in the design spec. Replaces
/// <see cref="JsonStringEnumConverter{TEnum}"/>, which writes PascalCase and
/// cannot be parameterized with a naming policy through an attribute.
/// </summary>
internal sealed class OutputFormatJsonConverter : JsonConverter<OutputFormat>
{
    public override OutputFormat Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value is null
            ? throw new JsonException("Expected string for output_format, got null.")
            : value.ToLowerInvariant() switch
        {
            "text" => OutputFormat.Text,
            "json" => OutputFormat.Json,
            "full" => OutputFormat.Full,
            _ => throw new JsonException($"Unknown output_format value: '{value}'. Expected text|json|full."),
        };
    }

    public override void Write(Utf8JsonWriter writer, OutputFormat value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString().ToLowerInvariant());
    }
}
