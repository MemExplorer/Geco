using System.Text.Json.Serialization;

namespace Geco.Core.Gemini.Rest.Models.Message;

// Optional: Add support for "responseMimeType"
public readonly record struct GenerationConfig(
	[property: JsonPropertyName("stopSequences")]
	[property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	string[]? StopSequences = null,

	[property: JsonPropertyName("candidateCount")]
	[property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	int? CandidateCount = null,

	[property: JsonPropertyName("maxOutputTokens")]
	[property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	uint? MaxOutputTokens = null,

	[property: JsonPropertyName("temperature")]
	[property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	float? Temperature = null,

	[property: JsonPropertyName("topP")]
	[property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	float? TopP = null,

	[property: JsonPropertyName("topK")]
	[property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	int? TopK = null,

	[property: JsonPropertyName("presencePenalty")]
	[property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	int? PresencePenalty = null,

	[property: JsonPropertyName("frequencyPenalty")]
	[property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	int? FrequencyPenalty = null,

	[property: JsonPropertyName("responseLogprobs")]
	[property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	bool? ResponseLogProbs = null,

	[property: JsonPropertyName("logprobs")]
	[property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	int? LogProbs = null
)
{
}
