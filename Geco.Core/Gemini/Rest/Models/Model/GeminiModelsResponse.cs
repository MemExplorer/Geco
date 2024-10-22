using System.Text.Json.Serialization;

namespace Geco.Core.Gemini.Rest.Models.Model;

public readonly record struct GeminiModelsResponse([property: JsonPropertyName("models")] GeminiModelResponse[] Models)
{
}
