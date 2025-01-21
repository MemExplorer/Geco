using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Geco.Core.Database;
using GoogleGeminiSDK;
using Microsoft.Extensions.AI;

namespace Geco.ViewModels;

public partial class WeeklyReportChatViewModel : ObservableObject, IQueryAttributable
{
	[ObservableProperty] ObservableCollection<ChatMessage> _chatMessages = [];
	[ObservableProperty] bool _isChatEnabled = true;
	string? HistoryId { get; set; }
	GeminiChat GeminiClient { get; }

	public WeeklyReportChatViewModel()
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
		var chatRepo = GlobalContext.Services.GetRequiredService<ChatRepository>();
		ChatMessages.Add(e.Message);

		// save chat message to database
		if (HistoryId != null)
			await chatRepo.AppendChat(HistoryId, e.Message);
	}

	[RelayCommand]
	async Task ChatSend(Editor inputEditor)
	{
		var geminiConfig = GlobalContext.Services.GetKeyedService<GeminiSettings>(GlobalContext.GeminiChat);

		// do not send an empty message
		if (string.IsNullOrWhiteSpace(inputEditor.Text))
			return;

		// hide keyboard after sending a message
		await inputEditor.HideSoftInputAsync(CancellationToken.None);
		string inputContent = inputEditor.Text;

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

		IsChatEnabled = true;
	}

	public void ApplyQueryAttributes(IDictionary<string, object> query)
	{
		if (!(query.TryGetValue("cdata", out object? obj) && obj is string historyId))
			return;

		var chatRepo = GlobalContext.Services.GetRequiredService<ChatRepository>();
		var currentGecoConversationTask = chatRepo.GetHistoryById(historyId);
		var currentGecoConversation = currentGecoConversationTask
			.ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext).GetAwaiter()
			.GetResult();
		var loadChatTask = chatRepo.LoadChats(currentGecoConversation);
		loadChatTask.ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
		HistoryId = currentGecoConversation.Id;
		ChatMessages = currentGecoConversation.Messages;
		GeminiClient.LoadHistory(currentGecoConversation.Messages);
	}
}
