
using CommunityToolkit.Mvvm.ComponentModel;
using Geco.Models.Chat;
using System.Collections.ObjectModel;

namespace Geco.ViewModels;

public partial class AppShellViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<ChatHistory> chatHistoryList;

    public AppShellViewModel()
    {
        chatHistoryList = [];
    }
}
