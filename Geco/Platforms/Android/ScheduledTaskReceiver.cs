using System.Text.Json;
using Android.App;
using Android.App.Usage;
using Android.Content;
using Android.Net;
using Android.Telephony;
using Geco.Core.Database;
using Geco.Core.Models.ActionObserver;
using Geco.Core.Models.Chat;
using Geco.Core.Models.Notification;
using GoogleGeminiSDK;
using Microsoft.Extensions.AI;
using AndroidOS = Android.OS;

namespace Geco;

[BroadcastReceiver(Exported = true, Enabled = true)]
[IntentFilter(["com.ssbois.geco.ScheduledTaskReceiver"])]
internal class ScheduledTaskReceiver : BroadcastReceiver
{
	static readonly INotificationManagerService NotificationSvc =
		GlobalContext.Services.GetRequiredService<INotificationManagerService>();

	public override async void OnReceive(Context? context, Intent? intent)
	{
		try
		{
			if (intent?.Action == "schedtaskcmd")
			{
				DeviceUsageMonitorService.CreateDeviceUsageScheduledLogger();
				await RunDeviceUsageLogger();
			}
			else if (intent?.Action == "weektasksummarycmd")
			{
				DeviceUsageMonitorService.CreateScheduledWeeklySummary();
				await RunWeeklySummaryNotification();
			}
		}
		catch (Exception ex)
		{
			GlobalContext.Logger.Error<ScheduledTaskReceiver>(ex);
		}
	}

	private async Task RunWeeklySummaryNotification()
	{
		try
		{
			GlobalContext.Logger.Info<ScheduledTaskReceiver>("Running weekly report...");
			var weeklyReportInfo = await SustainableReport.CreateWeeklyReport();
			if (weeklyReportInfo == null)
				throw new Exception("Function 'CreateWeeklyReport' returned null");

			var weeklyReportContent = weeklyReportInfo.Value.ReportDetails;
			string weeklyReportHtml = weeklyReportInfo.Value.FullReportContent;

			// Create chat history and save report as message
			var chatRepo = GlobalContext.Services.GetRequiredService<ChatRepository>();
			var chatMsg = new ChatMessage(new ChatRole("model"), weeklyReportHtml);
			chatMsg.AdditionalProperties = new AdditionalPropertiesDictionary { ["id"] = (ulong)0 };
			string chatTitle = weeklyReportContent.NotificationTitle;
			var historyInstance = new GecoConversation(Guid.NewGuid().ToString(),
				HistoryType.WeeklyReportConversation, chatTitle,
				DateTimeOffset.UtcNow.ToUnixTimeSeconds(), [chatMsg], weeklyReportContent.NotificationDescription,
				weeklyReportHtml);
			await chatRepo.AppendHistory(historyInstance);

			// Create notification
			NotificationSvc.SendInteractiveWeeklyReportNotification(historyInstance.Id,
				weeklyReportContent.NotificationTitle,
				weeklyReportContent.NotificationDescription,
				weeklyReportHtml);
			GlobalContext.Logger.Info<ScheduledTaskReceiver>("Finished running weekly report.");
		}
		catch (Exception ex)
		{
			GlobalContext.Logger.Error<ScheduledTaskReceiver>(ex,
				"Weekly report notification resulted into an error.");
		}
	}

	private async Task RunDeviceUsageLogger()
	{
		GlobalContext.Logger.Info<ScheduledTaskReceiver>("Running daily activity logger...");
		var triggerRepo = GlobalContext.Services.GetRequiredService<TriggerRepository>();
		var networkStatsManager = (NetworkStatsManager?)Platform.AppContext.GetSystemService("netstats");
		var usageStatsManager = (UsageStatsManager?)Platform.AppContext.GetSystemService("usagestats");
		if (usageStatsManager == null || networkStatsManager == null)
			throw new Exception("One of the services are null");

		var fetchSubId = await GetSubscriptionId();
		long currTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
		long dayBeforeTimestamp = currTime - 86_400_000; // subtract current time by 1 day in ms

		var queryStatsData = networkStatsManager.QuerySummaryForDevice(ConnectivityType.Mobile, fetchSubId.SubId,
			dayBeforeTimestamp, currTime);
		var queryStatsWifi =
			networkStatsManager.QuerySummaryForDevice(ConnectivityType.Wifi, fetchSubId.SubId, dayBeforeTimestamp,
				currTime);
		if (queryStatsData == null || queryStatsWifi == null)
			throw new Exception("Network query is null!");

		long totalScreenTimeMs = GetDeviceScreenTime(usageStatsManager);

		// check if screen time is equal or greater than 7 hrs in ms
		if (totalScreenTimeMs >= 25_200_000)
		{
			await triggerRepo.LogTrigger(DeviceInteractionTrigger.DeviceUsageUnsustainable, 1);
			await CreateUnsustainableNotification(DeviceInteractionTrigger.DeviceUsageUnsustainable);
		}
		else
			await triggerRepo.LogTrigger(DeviceInteractionTrigger.DeviceUsageSustainable, 1);

		// check if mobile data usage exceeds wifi usage
		if (queryStatsData.TxBytes > queryStatsWifi.TxBytes)
			await triggerRepo.LogTrigger(DeviceInteractionTrigger.NetworkUsageUnsustainable, 1);
		else
			await triggerRepo.LogTrigger(DeviceInteractionTrigger.NetworkUsageSustainable, 1);

		// Verify if the user has used geco search within the last 24 hours
		if (!await triggerRepo.IsTriggerInCooldown(DeviceInteractionTrigger.BrowserUsageSustainable, 86_400))
			await CreateUnsustainableNotification(DeviceInteractionTrigger.BrowserUsageUnsustainable);

		GlobalContext.Logger.Info<ScheduledTaskReceiver>("Finished running daily activity logger.");
	}

	// Inspired from https://stackoverflow.com/a/45380396
	private long GetDeviceScreenTime(UsageStatsManager usageStatsManager)
	{
		var appMap = new Dictionary<string, AppEventInfo>();
		var appStateMap = new Dictionary<string, UsageEvents.Event?>();

		// I have verified that local time is more accurate than UTC
		long currTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
		long startTime = currTime - 86_400_000; // querying past 1 day

		var usageEvents = usageStatsManager.QueryEvents(startTime, currTime);
		if (usageEvents == null)
			return 0;

		// capturing all events in a array to compare with next element
		// we assume that all subsequent events have a TimeStamp value that is equal to or greater than the previous one
		while (usageEvents.HasNextEvent)
		{
			var currentEvent = new UsageEvents.Event();
			usageEvents.GetNextEvent(currentEvent);
			switch (currentEvent.EventType)
			{
			case UsageEventType.MoveToForeground:
			case UsageEventType.MoveToBackground:
			case var evt
				when OperatingSystem.IsAndroidVersionAtLeast(29) && evt == UsageEventType.ActivityStopped:

				if (currentEvent.PackageName == null || currentEvent.ClassName == null)
					continue;

				string key = currentEvent.PackageName + currentEvent.ClassName;

				// taking it into a collection to access by package name
				if (!appMap.ContainsKey(key))
					appMap.Add(key, new AppEventInfo(currentEvent.PackageName, currentEvent.ClassName));

				bool appResumed = currentEvent.EventType == UsageEventType.MoveToForeground;
				bool hasKeyAppState = appStateMap.TryGetValue(key, out var currAppState);
				if (hasKeyAppState)
				{
					// The app is already running
					if (appResumed && currAppState != null)
						continue;

					// The app is either paused or stopped already
					if (!appResumed && currAppState == null)
						continue;
				}

				if (appResumed)
					appStateMap[key] = currentEvent;
				else
				{
					// skip when the first event is a paused or stopped event
					if (!hasKeyAppState)
						continue;

					// handle stop or pause
					long timeElapsed = currentEvent.TimeStamp - currAppState!.TimeStamp;
					appMap[key].TimeInForeground += timeElapsed;
					appStateMap[key] = null;
				}

				break;
			}
		}

		// log active app usage
		var groupedData = appMap.GroupBy(x => x.Value.PackageName, y => y.Value.TimeInForeground)
			.ToDictionary(x => x.Key, y => y.AsEnumerable().Sum());
		string activeAppsLogMessage = string.Join('\n', groupedData.OrderByDescending(x => x.Value)
			.Select(x => $"{x.Key} : {TimeSpan.FromMilliseconds(x.Value)}"));
		GlobalContext.Logger.Info<ScheduledTaskReceiver>($"Device Usage Info:\n{activeAppsLogMessage}");
		return appMap.Values.Sum(x => x.TimeInForeground);
	}

	class AppEventInfo(string PackageName, string ClassName)
	{
		public readonly string PackageName = PackageName;
		public string ClassName = ClassName;
		public long TimeInForeground { get; set; }
	}

	private async Task CreateUnsustainableNotification(DeviceInteractionTrigger triggerType)
	{
		var promptRepo = GlobalContext.Services.GetRequiredService<PromptRepository>();
		var geminiSettings = GlobalContext.Services.GetKeyedService<GeminiSettings>(GlobalContext.GeminiNotification);
		var geminiClient = GlobalContext.Services.GetRequiredService<GeminiChat>();
		string notificationPrompt = await promptRepo.GetPrompt(triggerType);
		try
		{
			GlobalContext.Logger.Info<ScheduledTaskReceiver>(
				$"Executing {triggerType} notification from daily activity logger.");
			await Utils.RetryAsyncTaskOrThrow<TaskCanceledException>(3, async () =>
			{
				var tunedNotification = await geminiClient.SendMessage(notificationPrompt, settings: geminiSettings);
				var deserializedStructuredMsg =
					JsonSerializer.Deserialize<List<TunedNotificationInfo>>(tunedNotification.Text!)!;
				var tunedNotificationInfoFirstEntry = deserializedStructuredMsg.First();
				NotificationSvc.SendInteractiveNotification(tunedNotificationInfoFirstEntry.NotificationTitle,
					tunedNotificationInfoFirstEntry.NotificationDescription,
					tunedNotificationInfoFirstEntry.FullContent);
			});
		}
		catch (Exception ex)
		{
			GlobalContext.Logger.Error<ScheduledTaskReceiver>(ex,
				"Daily activity notification resulted into an error.");
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
