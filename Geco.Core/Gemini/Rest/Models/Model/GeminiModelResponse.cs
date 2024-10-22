using System.Text.Json.Serialization;

namespace Geco.Core.Gemini.Rest.Models.Model;

public readonly record struct GeminiModelResponse(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("displayName")] string DisplayName,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("inputTokenLimit")] uint InputTokenLimit,
    [property: JsonPropertyName("outputTokenLimit")] uint OutputTokenLimit,
    [property: JsonPropertyName("supportedGenerationMethods")] string[] SupportedGenerationMethods,
    [property: JsonPropertyName("temperature")] float? Temperature,
    [property: JsonPropertyName("maxTemperature")] float? MaxTemperature,
    [property: JsonPropertyName("topP")] float? TopP,
    [property: JsonPropertyName("topK")] float? TopK
)
{
}
