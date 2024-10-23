using Geco.Core.Gemini.Rest;
using Geco.Core.Gemini.Rest.Models.Message;

namespace Geco.Core.Gemini;

public class GeminiClient
{
    GeminiRestClient GeminiRC { get; }

    List<MessageContent> ChatHistory { get; }

    public GeminiClient(string apiKey, string instructions = "", string model = "gemini-1.5-flash-latest")
    {
        GeminiRC = new GeminiRestClient(apiKey, model, instructions);
        ChatHistory = new List<MessageContent>();
    }

    public async Task<ChatMessage> Prompt(string message)
    {
        await GeminiRC.TextPrompt(message, ChatHistory);
        return ChatHistory.Last().ToChatMessage();
    }
}
