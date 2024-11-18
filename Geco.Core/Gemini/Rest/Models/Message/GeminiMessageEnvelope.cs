using System.Text.Json;
using System.Text.Json.Serialization;

namespace Geco.Core.Gemini.Rest.Models.Message;

public readonly record struct GeminiMessageEnvelope(
	[property: JsonPropertyName("contents")]
	List<MessageContent> Content,
	[property: JsonPropertyName("systemInstruction")]
	[property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	MessageContent? SystemInstructions,
	[property: JsonPropertyName("generationConfig")]
	[property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	GenerationConfig? GenerationConfig
)
{
	internal static string WrapMessage(List<MessageContent> messages, string? instructions, GenerationConfig? config)
	{
		MessageContent? geminiInstructions =
			instructions == null ? null : MessageContent.ConstructMessage(instructions, null);
		var wrappedMsg = new GeminiMessageEnvelope(messages, geminiInstructions, config);
		return JsonSerializer.Serialize(wrappedMsg);
	}
}
