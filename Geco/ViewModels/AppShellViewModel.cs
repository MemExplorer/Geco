
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Geco.Models.Chat;

namespace Geco.ViewModels;

public partial class AppShellViewModel : ObservableObject
{
	[ObservableProperty]
	private ObservableCollection<ChatHistory> chatHistoryList;

	public AppShellViewModel() => chatHistoryList = [];
}
