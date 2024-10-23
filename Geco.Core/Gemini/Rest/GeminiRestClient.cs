using Geco.Core.Gemini.Rest.Models.Message;
using Geco.Core.Gemini.Rest.Models.Model;
using Geco.Core.Gemini.Rest.Request;

namespace Geco.Core.Gemini.Rest;

public class GeminiRestClient(string apiKey, string model, string instructions = "")
{
    private APIRequest RequestInstance { get; } = new APIRequest();
    private string API_KEY { get; } = apiKey;
    private string SystemInstructions { get; } = instructions;
    private string SelectedModel { get; set; } = model;
    private const string BASE_URL = "https://generativelanguage.googleapis.com/v1beta";

    public async Task<GeminiModelResponse[]?> GetModels()
    {
        var getModelsResult = await RequestInstance.GetAsync<GeminiModelsResponse>($"{BASE_URL}/models?key={API_KEY}");
        if (!getModelsResult.Success)
        {
            throw new Exception("Failed to fetch models!");
        }

        return getModelsResult.Content.Models;
    }

    public async Task TextPrompt(string text, List<MessageContent> history)
    {
        var message = MessageContent.ConstructMessage(text);
        history.Add(message);

        string envelopeMsg = GeminiMessageEnvelope.WrapMessage(history, SystemInstructions);
        var promptResult = await RequestInstance.PostAsync<GeminiMessage>($"{BASE_URL}/models/{SelectedModel}:generateContent?key={API_KEY}", envelopeMsg);
        if (!promptResult.Success)
        {
            throw new Exception("Prompt Failed!");
        }

        history.Add(promptResult.Content.ExtractMessageContent());
    }
}
