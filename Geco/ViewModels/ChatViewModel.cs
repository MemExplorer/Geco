using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Geco.Core.Gemini;
using Geco.Models.Chat;

namespace Geco.ViewModels;

public partial class ChatViewModel : ObservableObject
{
	[ObservableProperty]
	private ObservableCollection<ChatMessage> chatMessages;

	private GeminiClient GeminiClient { get; }
	private bool LoadedFromHistory { get; set; }

	public ChatViewModel()
	{
		chatMessages = [];
		GeminiClient = new GeminiClient("API_KEY");
		LoadedFromHistory = false;
	}

	public void Reset()
	{
		ChatMessages = [];
		GeminiClient.ClearHistory();
		LoadedFromHistory = false;
	}

	public void LoadHistory(ChatHistory history)
	{
		ChatMessages = history.Messages;
		GeminiClient.ClearHistory();
		GeminiClient.LoadHistory([.. history.Messages]);
		LoadedFromHistory = true;
	}

	[RelayCommand]
	private async Task ChatSend(Entry inputEntry)
	{
		if (string.IsNullOrWhiteSpace(inputEntry.Text))
		{
			return;
		}

		if (ChatMessages.Count == 0 && !LoadedFromHistory)
		{
			var shellViewModel = (AppShellViewModel)Shell.Current.BindingContext;
			string chatTitle = CreateChatTitle(inputEntry.Text);
			shellViewModel.ChatHistoryList.Add(new(Guid.NewGuid().ToString(), chatTitle, ChatMessages));
		}

		string inputContent = inputEntry.Text;
		inputEntry.Text = string.Empty;
		ChatMessages.Add(new(inputContent, "User"));

		var response = await GeminiClient.Prompt(inputContent);
		ChatMessages.Add(response);
	}

	private static string CreateChatTitle(string message)
	{
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
