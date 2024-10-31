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
		bool newChat = ChatMessages.Count == 0 && HistoryId == null;
		if (newChat)
		{
			var shellViewModel = (AppShellViewModel)currentShell.BindingContext;
			string chatTitle = CreateChatTitle(inputEntry.Text);
			var historyInstance = new ChatHistory(Guid.NewGuid().ToString(), chatTitle,
				DateTimeOffset.UtcNow.ToUnixTimeSeconds(), ChatMessages);
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
