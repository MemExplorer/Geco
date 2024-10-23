using Geco.Core.Gemini.Rest.Models.Message;

namespace Geco.Core.Gemini;

public readonly record struct ChatMessage(string Text, string Role)
{
    public MessageContent ToRestMessage()
    {
        return MessageContent.ConstructMessage(Text, Role);
    }

    public static ChatMessage FromRestMessage(MessageContent msg)
    {
        return new(msg.ExtractMessage(), msg.Role);
    }
}
