using System.Text.Json.Serialization;

namespace Geco.Core.Gemini.Rest.Models.Message;

public readonly record struct CandidateResponse(
	[property: JsonPropertyName("content")]
	MessageContent Content,
	[property: JsonPropertyName("finishReason")]
	string FinishReason
);
