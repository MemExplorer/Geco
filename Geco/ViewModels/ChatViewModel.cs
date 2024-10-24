using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Geco.Core.Gemini;
using Geco.Models.Chat;

namespace Geco.ViewModels;

public partial class ChatViewModel : ObservableObject
{
	private GeminiClient GeminiClient { get; }
	private bool LoadedFromHistory { get; set; }

	[ObservableProperty]
	private ObservableCollection<ChatMessage> chatMessages;

	public ChatViewModel()
	{
		chatMessages = [];
		GeminiClient = new GeminiClient("API_KEY");
		LoadedFromHistory = false;
	}

	public void LoadHistory(ChatHistory history)
	{
		ChatMessages = history.Messages;
		GeminiClient.ClearHistory();
		GeminiClient.LoadHistory([.. history.Messages]);
		LoadedFromHistory = true;
	}

	public void Reset()
	{
		ChatMessages = [];
		GeminiClient.ClearHistory();
		LoadedFromHistory = false;
	}

	[RelayCommand]
	private async Task ChatSend(Entry inputEntry)
	{
		// do not send an empty message
		if (string.IsNullOrWhiteSpace(inputEntry.Text))
		{
			return;
		}

		// saves new instance of a chat
		if (ChatMessages.Count == 0 && !LoadedFromHistory)
		{
			var shellViewModel = (AppShellViewModel)Shell.Current.BindingContext;
			string chatTitle = CreateChatTitle(inputEntry.Text);
			shellViewModel.ChatHistoryList.Add(new(Guid.NewGuid().ToString(), chatTitle, ChatMessages));
		}

		// set input to empty string after sending a message
		string inputContent = inputEntry.Text;
		inputEntry.Text = string.Empty;

		// Add user's message to message list
		ChatMessages.Add(new(inputContent, "User"));

		// send user message to Gemini and append its response
		var response = await GeminiClient.Prompt(inputContent);
		ChatMessages.Add(response);
	}

	private static string CreateChatTitle(string message)
	{
		// For now, I think 17 is a good max length for a title
		const uint MAX_TITLE_LEN = 17;
		if (message.Length <= MAX_TITLE_LEN)
		{
			return message.Trim() + "...";
		}
		else
		{
			return message[..17].Trim() + "...";
		}
	}
}
