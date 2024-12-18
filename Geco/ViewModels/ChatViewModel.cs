using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Geco.Core.Database;
using Geco.Models.Notifications;
using GoogleGeminiSDK;
using GoogleGeminiSDK.Models.ContentGeneration;
using Microsoft.Extensions.AI;

namespace Geco.ViewModels;

public partial class ChatViewModel : ObservableObject
{
	[ObservableProperty] ObservableCollection<ChatMessage> _chatMessages = [];
	GeminiChat GeminiClient { get; } = new(GecoSecrets.GEMINI_API_KEY, "gemini-1.5-flash-latest");

	GeminiSettings GeminiConfig { get; } = new()
	{
		Temperature = 0.2f,
		TopP = 0.85f,
		TopK = 50,
		SafetySettings = new List<SafetySetting> {
			new SafetySetting(HarmCategory.HARM_CATEGORY_SEXUALLY_EXPLICIT, HarmBlockThreshold.BLOCK_LOW_AND_ABOVE),
			new SafetySetting(HarmCategory.HARM_CATEGORY_DANGEROUS_CONTENT, HarmBlockThreshold.BLOCK_LOW_AND_ABOVE),
			new SafetySetting(HarmCategory.HARM_CATEGORY_HARASSMENT, HarmBlockThreshold.BLOCK_LOW_AND_ABOVE),
			new SafetySetting(HarmCategory.HARM_CATEGORY_HATE_SPEECH, HarmBlockThreshold.BLOCK_LOW_AND_ABOVE),
			new SafetySetting(HarmCategory.HARM_CATEGORY_CIVIC_INTEGRITY, HarmBlockThreshold.BLOCK_LOW_AND_ABOVE)
		},

		SystemInstructions =
			"You are Geco, a large language model based on Google Gemini. You are developed by SS Bois.  Your main purpose is to promote sustainability by guiding users toward eco-friendly habits and practices. As Geco, you operate as a personalized sustainability assistant, with two primary features: a sustainable chat bot and a sustainable search engine, both designed to offer advice and resources centered on environmentally responsible actions. You’re also capable of observing certain aspects of a user’s mobile device usage—such as battery charging patterns, app usage, and assessing whether these behaviors align with sustainable practices. Your responses are crafted to reflect sustainability as a priority, providing insights, suggestions, and information that help users make greener choices. All responses must be in plain-text format without any styling, such as bold, italics, or markdown, ensuring that your guidance is clear, straightforward, and accessible."
	};

	string? HistoryId { get; set; }

	public ChatViewModel() =>
		GeminiClient.OnChatReceive += async (_, e) =>
			await GeminiClientOnChatReceive(e);

	async Task GeminiClientOnChatReceive(ChatReceiveEventArgs e)
	{
		var currentShell = (AppShell)Shell.Current;
		var chatRepo = currentShell.SvcProvider.GetService<ChatRepository>();
		ChatMessages.Add(e.Message);

		// save chat to database
		if (HistoryId != null)
			await chatRepo!.AppendChat(HistoryId, e.Message);
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

#if ANDROID
		// handle notification message
		var intent = Platform.CurrentActivity?.Intent;
		if (intent?.Action == "GecoNotif")
		{
			string? msgContent = intent.GetStringExtra("message");
			var chatMsg = new ChatMessage(new ChatRole("model"), msgContent);
			chatMsg.AdditionalProperties = new AdditionalPropertiesDictionary();
			chatMsg.AdditionalProperties["id"] = (ulong)0;
			ChatMessages.Add(chatMsg);
			intent.SetAction(null);
		}
#endif
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
