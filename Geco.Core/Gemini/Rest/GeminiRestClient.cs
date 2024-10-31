using Geco.Core.Gemini.Rest.Models.Message;
using Geco.Core.Gemini.Rest.Models.Model;
using Geco.Core.Gemini.Rest.Request;

namespace Geco.Core.Gemini.Rest;

/// <summary>
///     Low-level access to Google Gemini API
/// </summary>
/// <param name="apiKey"></param>
/// <param name="model"></param>
/// <param name="instructions"></param>
class GeminiRestClient(
	string apiKey,
	string model,
	string instructions = "",
	GenerationConfig? genConfig = null)
{
	const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta";
	ApiRequest RequestInstance { get; } = new();
	string ApiKey { get; } = apiKey;
	string SystemInstructions { get; } = instructions;
	string SelectedModel { get; } = model;

	/// <summary>
	///     Gets all the available model including its details
	/// </summary>
	/// <returns>
	///     A list of Gemini model including its description,
	///     limitations, and parameters
	/// </returns>
	/// <exception cref="Exception"></exception>
	internal async Task<GeminiModelResponse[]?> GetModels()
	{
		var getModelsResult = await RequestInstance.GetAsync<GeminiModelsResponse>($"{BaseUrl}/models?key={ApiKey}");
		if (!getModelsResult.Success)
		{
			throw new Exception("Failed to fetch models!");
		}

		return getModelsResult.Content.Models;
	}

	/// <summary>
	///     Sends a prompt to Gemini
	/// </summary>
	/// <param name="text">Message to Gemini</param>
	/// <param name="history">Contents of chat history</param>
	/// <exception cref="Exception"></exception>
	internal async Task TextPrompt(string text, List<MessageContent> history)
	{
		var message = MessageContent.ConstructMessage(text);
		history.Add(message);

		string envelopeMsg = GeminiMessageEnvelope.WrapMessage(history, SystemInstructions, genConfig);
		var promptResult =
			await RequestInstance.PostAsync<GeminiMessage>(
				$"{BaseUrl}/models/{SelectedModel}:generateContent?key={ApiKey}", envelopeMsg);
		if (!promptResult.Success)
		{
			throw new Exception("Prompt Failed!");
		}

		history.Add(promptResult.Content.ExtractMessageContent());
	}
}
