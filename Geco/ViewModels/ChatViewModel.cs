using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Geco.Core.Gemini;
using System.Collections.ObjectModel;

namespace Geco.ViewModels;

public partial class ChatViewModel : ObservableObject
{
    [ObservableProperty]
    ObservableCollection<ChatMessage> chatMessages;

    GeminiClient GeminiClient { get; }

    public ChatViewModel()
    {
        chatMessages = new ObservableCollection<ChatMessage>();
        GeminiClient = new GeminiClient("API_KEY");
    }

    [RelayCommand]
    async Task ChatSend(Entry inputEntry)
    {
        if (string.IsNullOrWhiteSpace(inputEntry.Text))
        {
            return;
        }

        string userMsg = inputEntry.Text;
        inputEntry.Text = string.Empty;
        ChatMessages.Add(new(userMsg, "User"));

        var response = await GeminiClient.Prompt(userMsg);
        ChatMessages.Add(response);
    }
}
