using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Geco.Core.Database;
using Geco.Core.Models.Chat;
using GoogleGeminiSDK;
using Microsoft.Extensions.AI;
using Syncfusion.Maui.Toolkit.Chips;

namespace Geco.ViewModels;

public partial class ChatViewModel : ObservableObject
{
	[ObservableProperty] ObservableCollection<ChatMessage> _chatMessages = [];
	[ObservableProperty] bool _isAutoCompleteVisible = true;
	[ObservableProperty] bool _isChatEnabled = true;
	GeminiChat GeminiClient { get; }
	string? HistoryId { get; set; }
	string? ActionTitle { get; set; }

	public ChatViewModel()
	{
		GeminiClient = GlobalContext.Services.GetRequiredService<GeminiChat>();
		GeminiClient.OnChatReceive += async (_, e) =>
			await GeminiClientOnChatReceive(e);
	}

	/// <summary>
	///     Handles chat send and receive events from Gemini
	/// </summary>
	/// <param name="e">Chat message data</param>
	async Task GeminiClientOnChatReceive(ChatReceiveEventArgs e)
	{
		// append received message to chat UI
		var currentShell = (AppShell)Shell.Current;
		var chatRepo = GlobalContext.Services.GetRequiredService<ChatRepository>();
		ChatMessages.Add(e.Message);

		// save chat message to database
		if (HistoryId != null)
			await chatRepo!.AppendChat(HistoryId, e.Message);

		if (e.Message.Role != ChatRole.User)
			IsChatEnabled = true;
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
			ActionTitle = intent.GetStringExtra("title");
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
		var geminiConfig = GlobalContext.Services.GetKeyedService<GeminiSettings>(GlobalContext.GeminiChat);

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
			IsChatEnabled = false;

			// send user message to Gemini and append its response
			await GeminiClient.SendMessage(inputContent, settings: geminiConfig);
		}
		catch (Exception ex)
		{
			GlobalContext.Logger.Error<ChatViewModel>(ex);
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
			var chatRepo = GlobalContext.Services.GetRequiredService<ChatRepository>();
			var shellViewModel = (AppShellViewModel)currentShell.BindingContext;
			string chatTitle = CreateChatTitle(gecoInitiated ? ActionTitle! : inputContent);
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
