using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using hitokoto_cli.Models;

namespace hitokoto_cli.Json;

/// <summary>
/// AOT-safe JSON context. Snake_case naming applies to both serialization
/// and deserialization, so <c>min_length</c> maps to <c>MinLength</c>, etc.
///
/// <see cref="Shared"/> mirrors the <see cref="JsonSourceGenerationOptionsAttribute"/>
/// settings but adds a relaxed <see cref="JavaScriptEncoder"/> so non-ASCII
/// (Chinese) is emitted verbatim instead of <c>\uXXXX</c> escapes. The default
/// <c>JsonSerializerContext.Default</c> options freeze on first access and
/// cannot be mutated, so callers use <see cref="Shared"/>.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(AppConfig))]
[JsonSerializable(typeof(HitokotoResponse))]
internal sealed partial class HitokotoJsonContext : JsonSerializerContext
{
    public static readonly HitokotoJsonContext Shared = new(new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    });
}
