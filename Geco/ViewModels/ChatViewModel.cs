using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Geco.Core.Database;
using Geco.Core.Gemini;

namespace Geco.ViewModels;

public partial class ChatViewModel : ObservableObject
{
	private GeminiClient GeminiClient { get; }
	private string? HistoryId { get; set; }

	[ObservableProperty]
	private ObservableCollection<ChatMessage> chatMessages;

	public ChatViewModel()
	{
		chatMessages = [];
		GeminiClient = new GeminiClient("API_KEY");
		HistoryId = null;
	}

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
	private async Task ChatSend(Entry inputEntry)
	{
		var currentShell = ((AppShell)Shell.Current);
		var chatRepo = currentShell.SvcProvider.GetService<ChatRepository>();

		// do not send an empty message
		if (string.IsNullOrWhiteSpace(inputEntry.Text))
			return;

		// hide keyboard after sending a message
		await inputEntry.HideSoftInputAsync(CancellationToken.None);

		// saves new instance of a chat
		bool newChat = ChatMessages.Count == 0 && HistoryId == null;
		if (newChat)
		{
			var shellViewModel = (AppShellViewModel)currentShell.BindingContext;
			string chatTitle = CreateChatTitle(inputEntry.Text);
			var historyInstance = new ChatHistory(Guid.NewGuid().ToString(), chatTitle, DateTimeOffset.UtcNow.ToUnixTimeSeconds(), ChatMessages);
			shellViewModel.ChatHistoryList.Add(historyInstance);
			await chatRepo!.AppendHistory(historyInstance);
			HistoryId = historyInstance.Id;
		}

		// set input to empty string after sending a message
		string inputContent = inputEntry.Text;
		inputEntry.Text = string.Empty;

		// Add user's message to message list
		ulong currentMsgId = (ulong)ChatMessages.Count;
		var userMsg = new ChatMessage(currentMsgId, inputContent, "User");
		ChatMessages.Add(userMsg);


		// send user message to Gemini and append its response
		var rawResponse = await GeminiClient.Prompt(inputContent);
		var chatResponse = rawResponse.ToChatMessage(currentMsgId + 1);
		ChatMessages.Add(chatResponse);

		// save chat to database
		await chatRepo!.AppendChat(HistoryId!, userMsg);
		await chatRepo!.AppendChat(HistoryId!, chatResponse);

		if (newChat)
			await currentShell.GoToAsync("//" + HistoryId);
	}

	private static string CreateChatTitle(string message)
	{
		// For now, I think 17 is a good max length for a title
		const uint MAX_TITLE_LEN = 17;
		if (message.Length <= MAX_TITLE_LEN)
			return message.Trim() + "...";
		else
			return message[..17].Trim() + "...";
	}
}
