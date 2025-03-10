using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Geco.Core.Database;
using Geco.Core.Models.ActionObserver;
using Geco.Core.Models.Chat;
using Application = Microsoft.Maui.Controls.Application;
#if ANDROID
using Geco.PermissionHelpers;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;
#endif

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
		var chatRepo = GlobalContext.Services.GetRequiredService<ChatRepository>();
		currShellViewModel.ChatHistoryList.Clear();
		await chatRepo.DeleteAllHistory(HistoryType.DefaultConversation);
	}

	[RelayCommand]
	async Task ClearWeeklyReports()
	{
		var currentShell = (AppShell)Shell.Current;

		// delete confirmation dialog
		bool deleteAns =
			await currentShell.DisplayAlert("", "Are you sure you want to clear your weekly reports?", "Yes", "No");

		if (!deleteAns)
			return;

		var chatRepo = GlobalContext.Services.GetRequiredService<ChatRepository>();
		await chatRepo.DeleteAllHistory(HistoryType.WeeklyReportConversation);
	}

	public void ToggleDarkMode(ToggledEventArgs e)
	{
		bool isDark = e.Value;
		Application.Current!.UserAppTheme = isDark ? AppTheme.Dark : AppTheme.Light;
		GecoSettings.DarkMode = isDark;
	}

	public void LoadSettings(Switch themeToggle, Switch monitorToggle, Switch notificationToggle)
	{
		themeToggle.IsToggled = GecoSettings.DarkMode;
		notificationToggle.IsToggled = GecoSettings.Notifications;

		_handlerLocked = true;
		monitorToggle.IsToggled = GecoSettings.Monitor;
		_handlerLocked = false; // Reset flag
	}

	// ReSharper disable once AsyncVoidMethod
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

		GecoSettings.Notifications = e.Value;
	}

	// ReSharper disable once AsyncVoidMethod
	public async void ToggleMonitor(Switch sender, ToggledEventArgs e, IPlatformActionObserver platformActionObserver)
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
			var appOpsMgr =
				(AppOpsManager?)Platform.AppContext.GetSystemService(Context.AppOpsService);
			if (appOpsMgr == null)
				throw new Exception("appOpsMgr is unexpectedly null");

			string currentAppPackageName = Platform.AppContext.PackageName!;

			var checkUsageStatusPermissionFunc = bool () =>
			{
				var usageStatsPermissionResult =
					OperatingSystem.IsAndroidVersionAtLeast(29)
						? appOpsMgr.UnsafeCheckOpNoThrow("android:get_usage_stats", Process.MyUid(),
							currentAppPackageName)
						: appOpsMgr.CheckOpNoThrow("android:get_usage_stats", Process.MyUid(), currentAppPackageName);
				return usageStatsPermissionResult == AppOpsManagerMode.Allowed;
			};

			if (!checkUsageStatusPermissionFunc())
			{
				bool reqUsageStats;
				try
				{
					reqUsageStats =
						await new SpecialPermissionWatcher(checkUsageStatusPermissionFunc,
							Settings.ActionUsageAccessSettings, currentAppPackageName).RequestAsync();
				}
				catch (ActivityNotFoundException)
				{
					// Some android versions do not support this intent with args
					reqUsageStats =
						await new SpecialPermissionWatcher(checkUsageStatusPermissionFunc,
							Settings.ActionUsageAccessSettings).RequestAsync();
				}

				if (!reqUsageStats)
				{
					sender.IsToggled = false;
					await Toast.Make("Please allow usage stats permission").Show();
					return;
				}
			}

			// Check battery optimization setting
			var powerMgr = (PowerManager?)Platform.AppContext.GetSystemService(Context.PowerService);
			if (powerMgr == null)
				throw new Exception("powerMgr is unexpectedly null");

			var checkPwrMgr = bool () => powerMgr.IsIgnoringBatteryOptimizations(currentAppPackageName);
			if (!checkPwrMgr())
			{
				bool reqIgnoreBatteryOptimization =
					await new SpecialPermissionWatcher(checkPwrMgr,
						Settings.ActionRequestIgnoreBatteryOptimizations, currentAppPackageName).RequestAsync();
				if (!reqIgnoreBatteryOptimization)
				{
					sender.IsToggled = false;
					await Toast.Make("Please disable battery optimization").Show();
					return;
				}
			}

			// check alarm manager permissions
			var alarmManager =
				(AlarmManager?)Platform.AppContext.GetSystemService(Context.AlarmService);
			if (alarmManager == null)
				throw new Exception("alarmManager is unexpectedly null");

			if (OperatingSystem.IsAndroidVersionAtLeast(31) && !alarmManager.CanScheduleExactAlarms())
			{
				bool reqAlarm =
					await new SpecialPermissionWatcher(alarmManager.CanScheduleExactAlarms,
						Settings.ActionRequestScheduleExactAlarm, currentAppPackageName).RequestAsync();
				if (!reqAlarm)
				{
					sender.IsToggled = false;
					await Toast.Make("Please allow schedule exact alarm permissions in settings").Show();
					return;
				}
			}
#endif
			platformActionObserver.Start();
		}
		else
			platformActionObserver.Stop();

		GecoSettings.Monitor = e.Value;
	}
}
