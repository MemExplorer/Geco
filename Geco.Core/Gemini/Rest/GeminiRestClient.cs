using Geco.Core.Gemini.Rest.Models.Message;
using Geco.Core.Gemini.Rest.Models.Model;
using Geco.Core.Gemini.Rest.Request;

namespace Geco.Core.Gemini.Rest;

/// <summary>
/// Low-level access to Google Gemini API
/// </summary>
/// <param name="apiKey"></param>
/// <param name="model"></param>
/// <param name="instructions"></param>
internal class GeminiRestClient(string apiKey, string model, string instructions = "", GenerationConfig? GenConfig = null)
{
	private const string BASE_URL = "https://generativelanguage.googleapis.com/v1beta";
	private APIRequest RequestInstance { get; } = new APIRequest();
	private string API_KEY { get; } = apiKey;
	private string SystemInstructions { get; } = instructions;
	private string SelectedModel { get; set; } = model;

	/// <summary>
	/// Gets all the available model including its details
	/// </summary>
	/// <returns>A list of Gemini model including its description, 
	/// limitations, and parameters</returns>
	/// <exception cref="Exception"></exception>
	internal async Task<GeminiModelResponse[]?> GetModels()
	{
		var getModelsResult = await RequestInstance.GetAsync<GeminiModelsResponse>($"{BASE_URL}/models?key={API_KEY}");
		if (!getModelsResult.Success)
		{
			throw new Exception("Failed to fetch models!");
		}

		return getModelsResult.Content.Models;
	}

	/// <summary>
	/// Sends a prompt to Gemini
	/// </summary>
	/// <param name="text">Message to Gemini</param>
	/// <param name="history">Contents of chat history</param>
	/// <exception cref="Exception"></exception>
	internal async Task TextPrompt(string text, List<MessageContent> history)
	{
		var message = MessageContent.ConstructMessage(text);
		history.Add(message);

		string envelopeMsg = GeminiMessageEnvelope.WrapMessage(history, SystemInstructions, GenConfig);
		var promptResult = await RequestInstance.PostAsync<GeminiMessage>($"{BASE_URL}/models/{SelectedModel}:generateContent?key={API_KEY}", envelopeMsg);
		if (!promptResult.Success)
		{
			throw new Exception("Prompt Failed!");
		}

		history.Add(promptResult.Content.ExtractMessageContent());
	}
}
