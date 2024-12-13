
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
#if ANDROID

			// check location permission
			var reqLocation = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
			if (reqLocation != PermissionStatus.Granted)
			{
				sender.IsToggled = false;
				await Toast.Make("Please allow location permission").Show();
				return;
			}

			// check usage stats permission
			var appOpsMgr = (Android.App.AppOpsManager?)Platform.AppContext.GetSystemService(Android.Content.Context.AppOpsService);
			if (appOpsMgr == null)
				throw new Exception("appOpsMgr is unexpectedly null");

			string currentAppPackageName = Platform.AppContext.PackageName!;

			var checkUsageStatusPermissionFunc = bool () =>
			{
				Android.App.AppOpsManagerMode usageStatsPermissionResult;
				if (OperatingSystem.IsAndroidVersionAtLeast(29))
					usageStatsPermissionResult = appOpsMgr.UnsafeCheckOpNoThrow("android:get_usage_stats", Android.OS.Process.MyUid(), currentAppPackageName);
				else
					usageStatsPermissionResult = appOpsMgr.CheckOpNoThrow("android:get_usage_stats", Android.OS.Process.MyUid(), currentAppPackageName);

				return usageStatsPermissionResult == Android.App.AppOpsManagerMode.Allowed;
			};

			if (!checkUsageStatusPermissionFunc())
			{
				var reqUsageStats = await new SpecialPermissionWatcher(checkUsageStatusPermissionFunc, Android.Provider.Settings.ActionUsageAccessSettings, currentAppPackageName).RequestAsync();
				if (!reqUsageStats)
				{
					sender.IsToggled = false;
					await Toast.Make("Please allow usage stats permission").Show();
					return;
				}
			}

			// check alarm manager permissions
			var alarmManager = (Android.App.AlarmManager?)Platform.AppContext.GetSystemService(Android.Content.Context.AlarmService);
			if (alarmManager == null)
				throw new Exception("alarmManager is unexpectedly null");

			if (OperatingSystem.IsAndroidVersionAtLeast(31) && !alarmManager.CanScheduleExactAlarms())
			{
				var reqAlarm = await new SpecialPermissionWatcher(alarmManager.CanScheduleExactAlarms, Android.Provider.Settings.ActionRequestScheduleExactAlarm, currentAppPackageName).RequestAsync();
				if (!reqAlarm)
				{
					sender.IsToggled = false;
					await Toast.Make("Please allow schedule exact alarm permissions in settings").Show();
					return;
				}
			}
#endif
			monitorManagerService.Start();
		}
		else
			monitorManagerService.Stop();

		Preferences.Set(nameof(GecoSettings.Monitor), e.Value);
	}
}
