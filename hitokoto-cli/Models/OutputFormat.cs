using System.Text.Json.Serialization;
using hitokoto_cli.Json;

namespace hitokoto_cli.Models;

/// <summary>
/// CLI rich output format for a parsed <see cref="HitokotoResponse"/>.
/// Serialized as a lowercase string in the config file (e.g. "full") via
/// <see cref="OutputFormatJsonConverter"/>, which is AOT-safe.
/// </summary>
[JsonConverter(typeof(OutputFormatJsonConverter))]
internal enum OutputFormat
{
    Text,
    Json,
    Full,
}
