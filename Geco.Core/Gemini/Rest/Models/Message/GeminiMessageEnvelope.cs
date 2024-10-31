using System.Text.Json;
using System.Text.Json.Serialization;

namespace Geco.Core.Gemini.Rest.Models.Message;

public readonly record struct GeminiMessageEnvelope(
	[property: JsonPropertyName("contents")]
	List<MessageContent> Content,
	[property: JsonPropertyName("systemInstruction")]
	MessageContent SystemInstructions,
	[property: JsonPropertyName("generationConfig")]
	[property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	GenerationConfig? GenerationConfig
)
{
	internal static string WrapMessage(List<MessageContent> messages, string instructions, GenerationConfig? config)
	{
		var wrappedMsg = new GeminiMessageEnvelope(messages, MessageContent.ConstructMessage(instructions), config);
		return JsonSerializer.Serialize(wrappedMsg);
	}
}
