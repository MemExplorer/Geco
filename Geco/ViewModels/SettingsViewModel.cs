using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Geco.Core.Database;
using Geco.Models;
using Geco.Models.Monitor;

namespace Geco.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
	private bool _isProgrammaticChange = false;


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


#pragma warning restore 1998
#pragma warning disable 1998
	public async void LoadSettings(Switch themeToggle, Switch monitorToggle, Switch notificationToggle)
	{
		themeToggle.IsToggled = Preferences.Get(nameof(GecoSettings.DarkMode), false);

#if ANDROID
		var permNotifStats = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
		var permBattStats = await Permissions.CheckStatusAsync<Permissions.Battery>();

		if (permNotifStats != PermissionStatus.Granted)
		{
			notificationToggle.IsToggled = false;
			return;
		}

		if (permBattStats != PermissionStatus.Granted)
		{
			monitorToggle.IsToggled = false;
			return;
		}

#endif

		_isProgrammaticChange = true; 
		notificationToggle.IsToggled = Preferences.Get(nameof(GecoSettings.Notifications), false);
		monitorToggle.IsToggled = Preferences.Get(nameof(GecoSettings.Monitor), false);
		_isProgrammaticChange = false; // Reset flag

	}
#pragma warning restore 1998
#pragma warning disable 1998
	public async void ToggleNotifications(Switch sender, ToggledEventArgs e)
	{
		if (e.Value)
		{
#if ANDROID
			var permStats = await Permissions.RequestAsync<Permissions.PostNotifications>();
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

#pragma warning disable 1998
	public async void ToggleMonitor(Switch sender, ToggledEventArgs e, IMonitorManagerService monitorManagerService)
	{
		if (_isProgrammaticChange)
			return; // Skip execution for programmatic changes

		if (e.Value)
		{
#if ANDROID
			var permBattStats = await Permissions.CheckStatusAsync<Permissions.Battery>();

			if (permBattStats != PermissionStatus.Granted)
			{
				sender.IsToggled = false;
				await Toast.Make("Please allow both battery and network state permissions in settings").Show();
				return;
			}
			else
			{
				monitorManagerService.Start();
			}
		}
		else
		{
			monitorManagerService.Stop();
#endif
		}


		Preferences.Set(nameof(GecoSettings.Monitor), e.Value);
	}
#pragma warning restore 1998




}
