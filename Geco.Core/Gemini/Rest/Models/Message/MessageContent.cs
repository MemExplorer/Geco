using System.Text.Json.Serialization;

namespace Geco.Core.Gemini.Rest.Models.Message;

public readonly record struct MessageContent(
	[property: JsonPropertyName("parts")] List<MessagePart> Parts,
	[property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	[property: JsonPropertyName("role")]
	string? Role
)
{
	internal string ExtractMessage()
	{
		// Text content is always at the first part
		var firstPart = Parts.First();
		return firstPart.Text;
	}

	public ChatMessage ToChatMessage(ulong currentId) => ChatMessage.FromRestMessage(currentId, this);

	internal static MessageContent ConstructMessage(string messageContent, string? role)
	{
		var msg = new MessagePart(messageContent);
		return new MessageContent([msg], role);
	}
}
