using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Geco.Core.Database;
using Geco.Models;
using Geco.Models.DeviceState;

namespace Geco.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
	private bool _handlerLocked;

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

	public void LoadSettings(Switch themeToggle, Switch monitorToggle, Switch notificationToggle)
	{
		themeToggle.IsToggled = Preferences.Get(nameof(GecoSettings.DarkMode), false);
		notificationToggle.IsToggled = Preferences.Get(nameof(GecoSettings.Notifications), false);

		_handlerLocked = true;
		monitorToggle.IsToggled = Preferences.Get(nameof(GecoSettings.Monitor), false);
		_handlerLocked = false; // Reset flag
	}

	public async void ToggleNotifications(Switch sender, ToggledEventArgs e)
	{
		if (OperatingSystem.IsAndroid() && e.Value)
		{
			var permStats = await Permissions.RequestAsync<Permissions.PostNotifications>();
			if (permStats != PermissionStatus.Granted)
			{
				sender.IsToggled = false;
				await Toast.Make("Please allow notification permissions in settings").Show();
				return;
			}
		}

		Preferences.Set(nameof(GecoSettings.Notifications), e.Value);
	}

	public async void ToggleMonitor(Switch sender, ToggledEventArgs e, IMonitorManagerService monitorManagerService)
	{
		if (_handlerLocked)
			return; // Skip execution when loading settings

		if (e.Value)
		{
			if (OperatingSystem.IsAndroid())
			{
				var permBattStats = await Permissions.CheckStatusAsync<Permissions.Battery>();

				if (permBattStats != PermissionStatus.Granted)
				{
					sender.IsToggled = false;
					await Toast.Make("Please allow both battery and network state permissions in settings").Show();
					return;
				}
			}

			monitorManagerService.Start();
		}
		else
			monitorManagerService.Stop();


		Preferences.Set(nameof(GecoSettings.Monitor), e.Value);
	}
}
