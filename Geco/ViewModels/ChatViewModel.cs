using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Geco.Core.Database;
using Geco.Models.Notifications;
using GoogleGeminiSDK;
using Microsoft.Extensions.AI;

namespace Geco.ViewModels;

public partial class ChatViewModel : ObservableObject
{
	[ObservableProperty] ObservableCollection<ChatMessage> _chatMessages = [];
	GeminiChat GeminiClient { get; } = new("API_KEY", "gemini-1.5-flash-latest");

	GeminiSettings GeminiConfig { get; } = new()
	{
		SystemInstructions =
			"You are Geco, a large language model based on Google Gemini. You are developed by SS Bois.  Your main purpose is to promote sustainability by guiding users toward eco-friendly habits and practices. As Geco, you operate as a personalized sustainability assistant, with two primary features: a sustainable chat bot and a sustainable search engine, both designed to offer advice and resources centered on environmentally responsible actions. You’re also capable of observing certain aspects of a user’s mobile device usage—such as battery charging patterns, app usage, and assessing whether these behaviors align with sustainable practices. Your responses are crafted to reflect sustainability as a priority, providing insights, suggestions, and information that help users make greener choices. All responses must be in plain-text format without any styling, such as bold, italics, or markdown, ensuring that your guidance is clear, straightforward, and accessible."
	};

	string? HistoryId { get; set; }

	public ChatViewModel() =>
		GeminiClient.OnChatReceive += async (s, e) =>
		await GeminiClientOnChatReceive(s, e);

	async Task GeminiClientOnChatReceive(object? sender, ChatReceiveEventArgs e)
	{
		var currentShell = (AppShell)Shell.Current;
		var chatRepo = currentShell.SvcProvider.GetService<ChatRepository>();
		ChatMessages.Add(e.Message);

		// save chat to database
		await chatRepo!.AppendChat(HistoryId!, e.Message);
	}

	public void LoadHistory(GecoChatHistory history)
	{
		ChatMessages = history.Messages;
		GeminiClient.LoadHistory(history.Messages);
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
		bool gecoInitiated = ChatMessages.Count == 1 && ChatMessages.First().Role != ChatRole.User;
		bool newChat = (ChatMessages.Count == 0 || gecoInitiated) && HistoryId == null;
		if (newChat)
		{
			var shellViewModel = (AppShellViewModel)currentShell.BindingContext;
			string chatTitle = CreateChatTitle(gecoInitiated ? ChatMessages.First().Text! : inputEntry.Text);
			var historyInstance = new GecoChatHistory(Guid.NewGuid().ToString(), chatTitle,
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

		// send user message to Gemini and append its response
		await GeminiClient.SendMessage(inputContent, settings: GeminiConfig);

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
