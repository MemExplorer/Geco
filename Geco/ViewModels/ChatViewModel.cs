using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Geco.Core.Database;
using Geco.Core.Gemini;

namespace Geco.ViewModels;

public partial class ChatViewModel : ObservableObject
{
	[ObservableProperty] ObservableCollection<ChatMessage> _chatMessages = [];
	GeminiClient GeminiClient { get; } = new("API_KEY");

	GeminiConfig GeminiConfig { get; } = new()
	{
		Conversational = true,
		Role = "User",
		SystemInstructions =
			"You are Geco, a large language model based on Google Gemini. You are developed by SS Bois.  Your main purpose is to promote sustainability by guiding users toward eco-friendly habits and practices. As Geco, you operate as a personalized sustainability assistant, with two primary features: a sustainable chat bot and a sustainable search engine, both designed to offer advice and resources centered on environmentally responsible actions. You’re also capable of observing certain aspects of a user’s mobile device usage—such as battery charging patterns, app usage, and assessing whether these behaviors align with sustainable practices. Your responses are crafted to reflect sustainability as a priority, providing insights, suggestions, and information that help users make greener choices. All responses must be in plain-text format without any styling, such as bold, italics, or markdown, ensuring that your guidance is clear, straightforward, and accessible."
	};

	string? HistoryId { get; set; }

	public void LoadHistory(ChatHistory history)
	{
		ChatMessages = history.Messages;
		GeminiClient.ClearHistory();
		GeminiClient.LoadHistory([.. history.Messages]);
		HistoryId = history.Id;
	}

	public void Reset()
	{
		ChatMessages = [];
		GeminiClient.ClearHistory();
		HistoryId = null;
	}

	[RelayCommand]
	async Task ChatSend(Entry inputEntry)
	{
		var currentShell = (AppShell)Shell.Current;
		var chatRepo = currentShell.SvcProvider.GetService<ChatRepository>();

		// do not send an empty message
		if (string.IsNullOrWhiteSpace(inputEntry.Text))
			return;

		// hide keyboard after sending a message
		await inputEntry.HideSoftInputAsync(CancellationToken.None);

		// saves new instance of a chat
		bool gecoInitiated = ChatMessages.Count == 1 && ChatMessages.First().IsSentByBot;
		bool newChat = (ChatMessages.Count == 0 || gecoInitiated) && HistoryId == null;
		if (newChat)
		{
			var shellViewModel = (AppShellViewModel)currentShell.BindingContext;
			string chatTitle = CreateChatTitle(gecoInitiated ? ChatMessages.First().Text : inputEntry.Text);
			var historyInstance = new ChatHistory(Guid.NewGuid().ToString(), chatTitle,
				DateTimeOffset.UtcNow.ToUnixTimeSeconds(), ChatMessages);

			// append to UI
			shellViewModel.ChatHistoryList.Add(historyInstance);

			// save to database
			await chatRepo!.AppendHistory(historyInstance);

			// set new history id
			HistoryId = historyInstance.Id;

			if (gecoInitiated)
				GeminiClient.AppendToHistory(ChatMessages.First());
		}

		// set input to empty string after sending a message
		string inputContent = inputEntry.Text;
		inputEntry.Text = string.Empty;

		// Add user's message to message list
		ulong currentMsgId = (ulong)ChatMessages.Count;
		var userMsg = new ChatMessage(currentMsgId, inputContent, "User");
		ChatMessages.Add(userMsg);


		// send user message to Gemini and append its response
		var rawResponse = await GeminiClient.Prompt(inputContent, GeminiConfig);
		var chatResponse = rawResponse.ToChatMessage(currentMsgId + 1);
		ChatMessages.Add(chatResponse);

		// save chat to database
		await chatRepo!.AppendChat(HistoryId!, userMsg);
		await chatRepo.AppendChat(HistoryId!, chatResponse);

		if (newChat)
			await currentShell.GoToAsync("//" + HistoryId);
	}

	static string CreateChatTitle(string message)
	{
		// For now, I think 17 is a good max length for a title
		const uint maxTitleLen = 17;
		if (message.Length <= maxTitleLen)
			return message.Trim() + "...";
		return message[..17].Trim() + "...";
	}
}
