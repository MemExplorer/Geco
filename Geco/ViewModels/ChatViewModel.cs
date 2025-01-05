using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Geco.Core.Database;
using Geco.Core.Models.Chat;
using GoogleGeminiSDK;
using GoogleGeminiSDK.Models.ContentGeneration;
using Microsoft.Extensions.AI;
using Syncfusion.Maui.Toolkit.Chips;

namespace Geco.ViewModels;

public partial class ChatViewModel : ObservableObject
{
	[ObservableProperty] ObservableCollection<ChatMessage> _chatMessages = [];
	[ObservableProperty] bool _isAutoCompleteVisible = true;
	GeminiChat GeminiClient { get; } = new(GecoSecrets.GEMINI_API_KEY, "gemini-1.5-flash-latest");

	GeminiSettings GeminiConfig { get; } = new()
	{
		Temperature = 0.2f,
		TopP = 0.85f,
		TopK = 50,
		SafetySettings = new List<SafetySetting>
		{
			new(HarmCategory.HARM_CATEGORY_SEXUALLY_EXPLICIT, HarmBlockThreshold.BLOCK_LOW_AND_ABOVE),
			new(HarmCategory.HARM_CATEGORY_DANGEROUS_CONTENT, HarmBlockThreshold.BLOCK_LOW_AND_ABOVE),
			new(HarmCategory.HARM_CATEGORY_HARASSMENT, HarmBlockThreshold.BLOCK_LOW_AND_ABOVE),
			new(HarmCategory.HARM_CATEGORY_HATE_SPEECH, HarmBlockThreshold.BLOCK_LOW_AND_ABOVE),
			new(HarmCategory.HARM_CATEGORY_CIVIC_INTEGRITY, HarmBlockThreshold.BLOCK_LOW_AND_ABOVE)
		},
		SystemInstructions =
			"You are GECO which stands for Green Efforts on Carbon, a large language model based on Google Gemini, and is currently only integrated in a mobile application. You are developed by SS Bois. Your main purpose is to promote sustainability by guiding users toward eco-friendly habits and practices. As GECO, you operate as a personalized sustainability assistant, with two primary features: a sustainable chat bot and a sustainable search engine. While both are designed to offer advice and resources centered on environmentally responsible actions, the difference lies between your tone. Sustainable chat has this conversation-like tone; On the other hand, sustainable search has a search engine-like manner of response. You’re also capable of observing certain aspects of a user’s mobile device usage. Specifically are these five: battery charging, screen time, use of location services, use of network services, and searching. You assess whether these habits align with sustainable practices and provide a weekly sustainability likelihood report if the Monitor habits is allowed in the settings. Your responses are crafted to reflect sustainability as a priority, providing insights, suggestions, and information that help users make greener choices. All responses must be in plain-text format without any styling, such as bold, italics, or markdown, ensuring that your guidance is clear, straightforward, and accessible. The application that you are in has the sustainable chat page as the starting page. On the upper left part of both sustainable chat and sustainable search is the navigation menu, that when toggled, shows the following navigation options in order: chat, search, conversation history, and at the bottom right of the navigation menu is the setting icon. In the settings page, the user may clear all conversations, change between light and dark mode, enable or disable mobile habit monitoring, and control notifications. Take note that you are currently utilized in the chat page."
	};

	string? HistoryId { get; set; }

	public ChatViewModel() =>
		GeminiClient.OnChatReceive += async (_, e) =>
			await GeminiClientOnChatReceive(e);

	/// <summary>
	///     Handles chat send and receive events from Gemini
	/// </summary>
	/// <param name="e">Chat message data</param>
	async Task GeminiClientOnChatReceive(ChatReceiveEventArgs e)
	{
		// append received message to chat UI
		var currentShell = (AppShell)Shell.Current;
		var chatRepo = currentShell.SvcProvider.GetService<ChatRepository>();
		ChatMessages.Add(e.Message);

		// save chat message to database
		if (HistoryId != null)
			await chatRepo!.AppendChat(HistoryId, e.Message);
	}

	/// <summary>
	///     Loads chat history into the ChatViewModel instance
	/// </summary>
	/// <param name="conversationInfo">Conversation data</param>
	public void LoadHistory(GecoConversation conversationInfo)
	{
		ChatMessages = conversationInfo.Messages;
		GeminiClient.LoadHistory(conversationInfo.Messages);
		HistoryId = conversationInfo.Id;
	}

	/// <summary>
	///     Resets current chat instance
	/// </summary>
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

	internal void ChipClick(SfChip chip, Editor chatEditor)
	{
		IsAutoCompleteVisible = false;
		switch (chip.Text)
		{
			case "Impacts of fast fashion":
				chatEditor.Text = "Can you tell me what are the impacts of the fast fashion to the environment?";
			break;
			case "Surprise me":
				chatEditor.Text = "Surprise me with anything about sustainability.";
			break;
			case "Sustainability Advice":
				chatEditor.Text = "Can you give me some advice related to being more sustainable?";
			break;
			case "Tutorial":
				chatEditor.Text = "Can you teach me how to use this application?";
			break;
		}
	}

	internal void ChatTextChanged(TextChangedEventArgs e)
	{
		if (!IsAutoCompleteVisible && e.NewTextValue.Length == 0)
			IsAutoCompleteVisible = true;
	}

	[RelayCommand]
	async Task ChatSend(Editor inputEditor)
	{
		var currentShell = (AppShell)Shell.Current;

		// do not send an empty message
		if (string.IsNullOrWhiteSpace(inputEditor.Text))
			return;

		// hide keyboard after sending a message
		await inputEditor.HideSoftInputAsync(CancellationToken.None);
		string inputContent = inputEditor.Text;
		bool isNewChat = await InitializeNewConversation(currentShell, inputContent);

		// set input to empty string after sending a message
		inputEditor.Text = string.Empty;

		try
		{
			// send user message to Gemini and append its response
			await GeminiClient.SendMessage(inputContent, settings: GeminiConfig);
		}
		catch
		{
			await Toast.Make("Failed sending message!").Show();
		}

		if (isNewChat)
			await currentShell.GoToAsync("//" + HistoryId);
	}

	async Task<bool> InitializeNewConversation(AppShell currentShell, string inputContent)
	{
		// saves new instance of a chat
		bool gecoInitiated = ChatMessages.Count == 1 && ChatMessages.First().Role != ChatRole.User;
		bool isNewChat = (ChatMessages.Count == 0 || gecoInitiated) && HistoryId == null;
		if (isNewChat)
		{
			var chatRepo = currentShell.SvcProvider.GetService<ChatRepository>();
			var shellViewModel = (AppShellViewModel)currentShell.BindingContext;
			string chatTitle = CreateChatTitle(gecoInitiated ? ChatMessages.First().Text! : inputContent);
			var historyInstance = new GecoConversation(Guid.NewGuid().ToString(), chatTitle,
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

		return isNewChat;
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
