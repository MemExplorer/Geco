using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Geco.Core.Database;
using Geco.Models;

namespace Geco.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
	[RelayCommand]
	async Task ClearHistory()
	{
		var currentShell = (AppShell)Shell.Current;

		// delete confirmation dialog
		bool deleteAns =
			await currentShell.DisplayAlert("", "Are you sure you want to clear history?", "Yes", "No");

		if (!deleteAns)
			return;

		var currShellViewModel = (AppShellViewModel)currentShell.BindingContext;
		var chatRepo = currentShell.SvcProvider.GetService<ChatRepository>();
		currShellViewModel.ChatHistoryList.Clear();
		await chatRepo!.DeleteAllHistory();
	}

	[RelayCommand]
	void ToggleDarkMode(ToggledEventArgs e)
	{
		bool isDark = e.Value;
		Application.Current!.UserAppTheme = isDark ? AppTheme.Dark : AppTheme.Light;
		Preferences.Set(nameof(GecoSettings.DarkMode), isDark);
	}

	[RelayCommand]
	void ToggleMonitor(ToggledEventArgs e) => Preferences.Set(nameof(GecoSettings.Monitor), e.Value);

#pragma warning disable 1998
	public async void LoadSettings(Switch themeToggle, Switch monitorToggle, Switch notificationToggle)
	{
		themeToggle.IsToggled = Preferences.Get(nameof(GecoSettings.DarkMode), false);
		monitorToggle.IsToggled = Preferences.Get(nameof(GecoSettings.Monitor), false);

#if ANDROID
		var permStats = await Permissions.CheckStatusAsync<NotificationPermission>();
		if (permStats != PermissionStatus.Granted)
		{
			notificationToggle.IsToggled = false;
			return;
		}
#endif

		notificationToggle.IsToggled = Preferences.Get(nameof(GecoSettings.Notifications), false);
	}
#pragma warning restore 1998
#pragma warning disable 1998
	public async void ToggleNotifications(Switch sender, ToggledEventArgs e)
	{
		if (e.Value)
		{
#if ANDROID
			var permStats = await Permissions.RequestAsync<NotificationPermission>();
			if (permStats != PermissionStatus.Granted)
			{
				sender.IsToggled = false;
				await Toast.Make("Please allow notification permissions in settings").Show();
				return;
			}

#endif
		}

		Preferences.Set(nameof(GecoSettings.Notifications), e.Value);
	}
#pragma warning restore 1998
}
