using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Geco.Core.Database;
using Geco.Core.Models.Chat;

namespace Geco.ViewModels;

public partial class AppShellViewModel : ObservableObject
{
	[ObservableProperty] ObservableCollection<GecoChatHistory> _chatHistoryList;

	[ObservableProperty] bool _isChatInstance;

	[ObservableProperty] bool _isChatPage;

	[ObservableProperty] string _pageTitle;

	public AppShellViewModel()
	{
		ChatHistoryList = [];
		IsChatPage = false;
		IsChatInstance = false;
		PageTitle = "Geco";
	}

	[RelayCommand]
	async Task GotoSettings()
	{
		var currentShell = (AppShell)Shell.Current;

		// close flyout
		currentShell.FlyoutIsPresented = false;

		// navigate to settings
		await Shell.Current.GoToAsync("SettingsPage");
	}

	[RelayCommand]
	async Task DeleteChat()
	{
		var currentShell = (AppShell)Shell.Current;

		// delete confirmation dialog
		bool deleteAns =
			await currentShell.DisplayAlert("", "Are you sure you want to delete this conversation?", "Yes", "No");

		if (!deleteAns)
			return;

		// get selected chat
		var chatRepo = currentShell.SvcProvider.GetService<ChatRepository>();
		string? currentPageId = currentShell.CurrentPage.Parent.ClassId;
		var selectedChat = ChatHistoryList.First(x => x.Id == currentPageId);

		// delete history in flyout
		ChatHistoryList.Remove(selectedChat);

		// delete history in database
		await chatRepo!.DeleteHistory(currentPageId);

		// go to new chat page
		await currentShell.GoToAsync("//ChatPage");
	}
}
