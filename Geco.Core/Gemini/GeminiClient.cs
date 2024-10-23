using Geco.Core.Gemini.Rest;
using Geco.Core.Gemini.Rest.Models.Message;

namespace Geco.Core.Gemini;

public class GeminiClient(string apiKey, string instructions = "", string model = "gemini-1.5-flash-latest")
{
    private GeminiRestClient GeminiRC { get; } = new GeminiRestClient(apiKey, model, instructions);

    private List<MessageContent> ChatHistory { get; } = [];

    public void LoadHistory(List<ChatMessage> messages)
    {
        ChatHistory.AddRange(messages.Select(x => x.ToRestMessage()));
    }

    public async Task<ChatMessage> Prompt(string message)
    {
        await GeminiRC.TextPrompt(message, ChatHistory);
        return ChatHistory.Last().ToChatMessage();
    }

    public void ClearHistory()
    {
        ChatHistory.Clear();
    }
}
