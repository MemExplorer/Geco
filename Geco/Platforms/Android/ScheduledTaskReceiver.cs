
using Android.App;
using Android.App.Usage;
using Android.Content;
using Android.Net;
using Android.Telephony;
using Geco.Core.Database;
using Geco.Models.Notifications;
using AndroidOS = Android.OS;

namespace Geco;

[BroadcastReceiver(Exported = true, Enabled = true)]
[IntentFilter(new[] { "com.ssbois.geco.ScheduledTaskReceiver" })]
internal class ScheduledTaskReceiver : BroadcastReceiver
{
	public override async void OnReceive(Context? context, Intent? intent)
	{
		if (intent?.Action != "schedtaskcmd")
			return;

		var SvcProvider = App.Current?.Handler.MauiContext?.Services!;
		var NotificationSvc = SvcProvider.GetService<INotificationManagerService>()!;
		var triggerRepo = SvcProvider.GetService<TriggerRepository>();
		if (triggerRepo == null)
			throw new Exception("TriggerRepository should not be null!");

		var networkStatsManager = (NetworkStatsManager?)Platform.AppContext.GetSystemService("netstats");
		var usageStatsManager = (UsageStatsManager?)Platform.AppContext.GetSystemService("usagestats");
		if (usageStatsManager == null || networkStatsManager == null)
			throw new Exception("One of the services are null");

		var fetchSubId = await GetSubscriptionId();
		var currTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
		var dayBeforeTimestamp = currTime - 86_400_000; // subtract current time by 1 day in ms
		var usageQueryStatsResult = usageStatsManager?.QueryUsageStats(UsageStatsInterval.Daily, dayBeforeTimestamp, currTime);
		if (usageQueryStatsResult == null)
			throw new Exception("Usage stats query is null");

		var queryStatsData = networkStatsManager.QuerySummaryForDevice(ConnectivityType.Mobile, fetchSubId.SubId, dayBeforeTimestamp, currTime);
		var queryStatsWifi = networkStatsManager.QuerySummaryForDevice(ConnectivityType.Wifi, fetchSubId.SubId, dayBeforeTimestamp, currTime);
		if (queryStatsData == null || queryStatsWifi == null)
			throw new Exception("Network query is null!");

		long totalScreenTime = 0;
		foreach (var s in usageQueryStatsResult)
		{
			if (s.TotalTimeInForeground > 0)
				totalScreenTime += s.TotalTimeInForeground;
		}

		// check if screen time is equal or greater than 7 hrs in ms
		if (totalScreenTime >= 25_200_000)
			await triggerRepo.LogTrigger(DeviceInteractionTrigger.DeviceUsageUnsustainable, 1);
		else
			await triggerRepo.LogTrigger(DeviceInteractionTrigger.DeviceUsageSustainable, 1);

		// check if mobile data usage exceeds wifi usage
		if (queryStatsData.TxBytes > queryStatsWifi.TxBytes)
			await triggerRepo.LogTrigger(DeviceInteractionTrigger.NetworkUsageUnsustainable, 1);
		else
			await triggerRepo.LogTrigger(DeviceInteractionTrigger.NetworkUsageSustainable, 1);
	}

	internal static async Task<(bool Granted, string? SubId)> GetSubscriptionId()
	{
		// for API 29 and above, return null since getting the SubscriberId is restricted
		if (AndroidOS.Build.VERSION.SdkInt < AndroidOS.BuildVersionCodes.Q)
		{
			var readPhoneStatePerms = await Permissions.RequestAsync<Permissions.Phone>();
			if (readPhoneStatePerms != PermissionStatus.Granted)
				return (false, null);

			var telephonyManager = (TelephonyManager?)Platform.AppContext.GetSystemService("phone");
			if (telephonyManager == null)
				return (false, null);

			return (true, telephonyManager.SubscriberId);
		}

		return (true, null);
	}
}
