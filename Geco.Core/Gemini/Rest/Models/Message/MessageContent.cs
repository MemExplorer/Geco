using System.Text.Json.Serialization;

namespace Geco.Core.Gemini.Rest.Models.Message;

public readonly record struct MessageContent(
	[property: JsonPropertyName("parts")] List<MessagePart> Parts,
	[property: JsonPropertyName("role")] string Role
)
{
	internal string ExtractMessage()
	{
		// Text content is always at the first part
		var firstPart = Parts.First();
		return firstPart.Text;
	}

	internal ChatMessage ToChatMessage()
	{
		return ChatMessage.FromRestMessage(this);
	}

	internal static MessageContent ConstructMessage(string messageContent, string? role = null)
	{
		var msg = new MessagePart(messageContent);
		return new([msg], role ?? "User");
	}
}
