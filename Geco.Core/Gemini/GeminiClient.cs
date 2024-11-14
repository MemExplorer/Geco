using Geco.Core.Gemini.Rest;
using Geco.Core.Gemini.Rest.Models.Message;

namespace Geco.Core.Gemini;

/// <summary>
///     Provides high-level access to Google Gemini
///     while also allowing users to save conversations through chat history
/// </summary>
/// <param name="apiKey">Your Gemini API Key</param>
/// <param name="model">
///     Specify Gemini Model to use. <br></br>See available models in
///     <see href="https://ai.google.dev/gemini-api/docs/models/gemini" />
/// </param>
public class GeminiClient(
	string apiKey,
	string model = "gemini-1.5-flash-latest")
{
	GeminiRestClient GeminiRc { get; } = new(apiKey, model);
	List<MessageContent> ChatHistory { get; } = [];

	/// <summary>
	///     Sends a prompt to Gemini
	/// </summary>
	/// <param name="message">Message to Gemini</param>
	/// <param name="config">Gemini configuration settings</param>
	/// <returns>Gemini's response.</returns>
	public async Task<MessageContent> Prompt(string message, GeminiConfig config)
	{
		var conversation = config.Conversational ? ChatHistory : [];
		await GeminiRc.TextPrompt(message, conversation, config);
		return conversation.Last();
	}

	/// <summary>
	/// Appends a message to history
	/// </summary>
	/// <param name="chatMessage">Message that will be appended</param>
	public void AppendToHistory(ChatMessage chatMessage) =>
		ChatHistory.Add(chatMessage.ToRestMessage());

	/// <summary>
	///     Loads history from list of chat messages
	/// </summary>
	/// <param name="messages">The whole chat conversation</param>
	public void LoadHistory(List<ChatMessage> messages) =>
		ChatHistory.AddRange(messages.Select(x => x.ToRestMessage()));

	/// <summary>
	///     Clears chat history
	/// </summary>
	public void ClearHistory() => ChatHistory.Clear();
}
