using CommunityToolkit.Mvvm.ComponentModel;
using Geco.Models;
using System.Collections.ObjectModel;

namespace Geco.ViewModels;

public partial class ChatViewModel : ObservableObject
{
    public ChatViewModel()
    {
        chatMessages = new ObservableCollection<ChatMessage>();
    }

    [ObservableProperty]
    ObservableCollection<ChatMessage> chatMessages;
}
