
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
[IntentFilter(["com.ssbois.geco.ScheduledTaskReceiver"])]
internal class ScheduledTaskReceiver : BroadcastReceiver
{
	public override async void OnReceive(Context? context, Intent? intent)
	{
		try
		{
			if (intent?.Action != "schedtaskcmd")
				return;

			var svcProvider = App.Current?.Handler.MauiContext?.Services!;
			var triggerRepo = svcProvider.GetService<TriggerRepository>();
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

			long totalScreenTime = usageQueryStatsResult.Where(s => s.TotalTimeInForeground > 0).Sum(s => s.TotalTimeInForeground);

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
		catch
		{
			// do nothing
		}
	}

	internal static async Task<(bool Granted, string? SubId)> GetSubscriptionId()
	{
		// for API 29 and above, return null since getting the SubscriberId is restricted
		if (AndroidOS.Build.VERSION.SdkInt >= AndroidOS.BuildVersionCodes.Q) 
			return (true, null);
		
		var readPhoneStatePerms = await Permissions.RequestAsync<Permissions.Phone>();
		if (readPhoneStatePerms != PermissionStatus.Granted)
			return (false, null);

		var telephonyManager = (TelephonyManager?)Platform.AppContext.GetSystemService("phone");
		return telephonyManager == null ? (false, null) : (true, telephonyManager.SubscriberId);
	}
}
