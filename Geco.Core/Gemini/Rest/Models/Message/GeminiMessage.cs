using System.Text.Json.Serialization;

namespace Geco.Core.Gemini.Rest.Models.Message;

public readonly record struct GeminiMessage(
	[property: JsonPropertyName("candidates")] CandidateResponse[] CandidateResponses
)
{
	public MessageContent ExtractMessageContent()
	{
		// Always pick the first candidate response
		var candResp = CandidateResponses.First();
		return candResp.Content;
	}
}
