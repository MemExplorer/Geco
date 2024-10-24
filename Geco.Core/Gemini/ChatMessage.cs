using Geco.Core.Gemini.Rest.Models.Message;

namespace Geco.Core.Gemini;

/// <summary>
/// Information about a single chat message
/// </summary>
/// <param name="Text">Contains contents of a message</param>
/// <param name="Role">Role of the sender</param>
public readonly record struct ChatMessage(string Text, string Role)
{
	internal MessageContent ToRestMessage()
	{
		return MessageContent.ConstructMessage(Text, Role);
	}

	internal static ChatMessage FromRestMessage(MessageContent msg)
	{
		return new(msg.ExtractMessage(), msg.Role);
	}
}
