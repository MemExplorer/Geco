using Geco.Core.Gemini.Rest.Models.Model;

namespace Geco.Core.Gemini.Rest;

public class GeminiRestClient
{
    private Request _request { get; }
    private string _API_KEY {get;}
    const string BASE_URL = "https://generativelanguage.googleapis.com/v1beta";

    public GeminiRestClient(string apiKey)
    {
        _request = new Request();
        _API_KEY = apiKey;
    }

    public async Task<GeminiModelResponse[]?> GetModels()
    {
        var getModelsResult = await _request.GetAsync<GeminiModelsResponse>($"{BASE_URL}/models?key={_API_KEY}");
        if (!getModelsResult.Success)
        {
            throw new Exception("Failed to fetch models!");
        }

        return getModelsResult.Content.Models;
    }
}
