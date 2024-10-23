using Geco.Core.Gemini.Rest.Models.Message;
using Geco.Core.Gemini.Rest.Models.Model;
using Geco.Core.Gemini.Rest.Request;

namespace Geco.Core.Gemini.Rest;

public class GeminiRestClient
{
    APIRequest RequestInstance { get; }
    string API_KEY { get; }
    string SystemInstructions { get; }
    string SelectedModel { get; set; }
    const string BASE_URL = "https://generativelanguage.googleapis.com/v1beta";

    public GeminiRestClient(string apiKey, string model, string instructions = "")
    {
        RequestInstance = new APIRequest();
        API_KEY = apiKey;
        SelectedModel = model;
        SystemInstructions = instructions;
    }

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

        var envelopeMsg = GeminiMessageEnvelope.WrapMessage(history, SystemInstructions);
        var promptResult = await RequestInstance.PostAsync<GeminiMessage>($"{BASE_URL}/models/{SelectedModel}:generateContent?key={API_KEY}", envelopeMsg);
        if (!promptResult.Success)
        {
            throw new Exception("Prompt Failed!");
        }

        history.Add(promptResult.Content.ExtractMessageContent());
    }
}
