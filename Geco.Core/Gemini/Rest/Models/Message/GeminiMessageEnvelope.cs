using System.Text.Json;
using System.Text.Json.Serialization;

namespace Geco.Core.Gemini.Rest.Models.Message;

public readonly record struct GeminiMessageEnvelope(
	[property: JsonPropertyName("contents")] List<MessageContent> Content,
	[property: JsonPropertyName("systemInstruction")] MessageContent SystemInstructions
)
{
	internal static string WrapMessage(List<MessageContent> messages, string instructions)
	{
		var wrappedMsg = new GeminiMessageEnvelope(messages, MessageContent.ConstructMessage(instructions));
		return JsonSerializer.Serialize(wrappedMsg);
	}
}
