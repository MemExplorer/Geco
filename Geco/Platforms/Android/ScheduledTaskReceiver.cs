using System.Globalization;
using System.Text.Json;
using Android.App;
using Android.App.Usage;
using Android.Content;
using Android.Net;
using Android.Telephony;
using Geco.Core;
using Geco.Core.Database;
using Geco.Core.Models.ActionObserver;
using Geco.Core.Models.Notification;
using GoogleGeminiSDK;
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
				await RunDeviceUsageLogger();
				DeviceUsageMonitorService.CreateDeviceUsageScheduledLogger();
			}
			else if (intent?.Action == "weektasksummarycmd")
			{
				await RunWeeklySummaryNotification();
				DeviceUsageMonitorService.CreateScheduledWeeklySummary();
			}
		}
		catch (Exception ex)
		{
			GlobalContext.Logger.Error<ScheduledTaskReceiver>(ex);
		}
	}

	private async Task RunWeeklySummaryNotification()
	{
		GlobalContext.Logger.Info<ScheduledTaskReceiver>("Running weekly report...");
		string? likelihoodPrompt = await ConstructLikelihoodPrompt();
		if (likelihoodPrompt == null)
			return;

		GlobalContext.Logger.Info<ScheduledTaskReceiver>("Created weekly report likelihood prompt.");
		try
		{
			GlobalContext.Logger.Info<ScheduledTaskReceiver>("Executing Scheduled Weekly Summary Notification.");
			var geminiClient = GlobalContext.Services.GetRequiredService<GeminiChat>();
			var geminiSettings =
				GlobalContext.Services.GetKeyedService<GeminiSettings>(GlobalContext.GeminiNotification);
			await Utils.RetryAsyncTaskOrThrow<TaskCanceledException>(3, async () =>
			{
				var weeklyReportResponse = await geminiClient.SendMessage(likelihoodPrompt, settings: geminiSettings);
				var deserializedWeeklyReport =
					JsonSerializer.Deserialize<List<TunedNotificationInfo>>(weeklyReportResponse.Text!)!;
				var firstItem = deserializedWeeklyReport.First();
				NotificationSvc.SendInteractiveNotification(firstItem.NotificationTitle, firstItem.NotificationDescription,
					firstItem.FullContent);
			});
		}
		catch (Exception ex)
		{
			GlobalContext.Logger.Error<ScheduledTaskReceiver>(ex, "Weekly summary notification resulted into an error.");
		}
	}

	private async Task<string?> ConstructLikelihoodPrompt()
	{
		// fetch trigger records for current week
		var triggerRepo = GlobalContext.Services.GetRequiredService<TriggerRepository>();
		var promptRepo = GlobalContext.Services.GetRequiredService<PromptRepository>();
		var currentWeekTriggerRecords = (await triggerRepo.FetchWeekOneTriggerRecords()).ToDictionary();
		var currentWeekResult = GetLikelihoodPromptFromRecords(currentWeekTriggerRecords);
		if (currentWeekResult == null)
			return null;

		// check if we have data from last 2 weeks
		if (await triggerRepo.HasHistory())
		{
			// fetch data from last 2 weeks
			var lastWeekTriggerRecords = (await triggerRepo.FetchWeekTwoTriggerRecords()).ToDictionary();
			var lastWeekResult = GetLikelihoodPromptFromRecords(lastWeekTriggerRecords);
			if (lastWeekResult != null)
			{
				// build likelihood prompt
				return await promptRepo.GetLikelihoodWithHistoryPrompt(
					currentWeekResult.Value.Probability, currentWeekResult.Value.PositiveComputation,
					currentWeekResult.Value.Frequency,
					lastWeekResult.Value.Probability, lastWeekResult.Value.PositiveComputation,
					lastWeekResult.Value.Frequency);
			}
		}

		return await promptRepo.GetLikelihoodPrompt(currentWeekResult.Value.Probability,
			currentWeekResult.Value.PositiveComputation,
			currentWeekResult.Value.Frequency);
	}

	private (string PositiveComputation, string Frequency, string Probability)? GetLikelihoodPromptFromRecords(
		Dictionary<DeviceInteractionTrigger, int> currentWeekTriggerRecords)
	{
		int chargingPositive =
			currentWeekTriggerRecords.GetValueOrDefault(DeviceInteractionTrigger.ChargingSustainable, 0);
		int chargingNegative =
			currentWeekTriggerRecords.GetValueOrDefault(DeviceInteractionTrigger.ChargingUnsustainable, 0);
		int networkUsagePositive =
			currentWeekTriggerRecords.GetValueOrDefault(DeviceInteractionTrigger.NetworkUsageSustainable, 0);
		int networkUsageNegative =
			currentWeekTriggerRecords.GetValueOrDefault(DeviceInteractionTrigger.NetworkUsageUnsustainable, 0);
		int deviceUsagePositive =
			currentWeekTriggerRecords.GetValueOrDefault(DeviceInteractionTrigger.DeviceUsageSustainable, 0);
		int deviceUsageNegative =
			currentWeekTriggerRecords.GetValueOrDefault(DeviceInteractionTrigger.DeviceUsageUnsustainable, 0);

		if (chargingPositive == 0 && chargingNegative == 0)
			return null;

		if (networkUsagePositive == 0 && networkUsageNegative == 0)
			return null;

		if (deviceUsagePositive == 0 && deviceUsageNegative == 0)
			return null;

		// pass the first week's data to Bayes Math Model
		var currWeekBayesInst = new BayesTheorem();
		currWeekBayesInst.AppendData("Charging", chargingPositive, chargingNegative);
		currWeekBayesInst.AppendData("Network Usage", networkUsagePositive, networkUsageNegative);
		currWeekBayesInst.AppendData("Device Usage", deviceUsagePositive, deviceUsageNegative);

		// gets values we need for prompt
		var currWeekComputationResult = currWeekBayesInst.Compute();
		var currWeekComputationStr = currWeekBayesInst.GetComputationInString();
		string currWeekFrequencyStr = currWeekBayesInst.GetFrequencyInString();
		string currSustainableProportionalProbability = Math.Round(currWeekComputationResult.PositiveProbs, 2)
			.ToString(CultureInfo.InvariantCulture) + "%";

		return (currWeekComputationStr.PositiveComputation, currWeekFrequencyStr,
			currSustainableProportionalProbability);
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
		var usageQueryStatsResult =
			usageStatsManager.QueryUsageStats(UsageStatsInterval.Daily, dayBeforeTimestamp, currTime);
		if (usageQueryStatsResult == null)
			throw new Exception("Usage stats query is null");

		var queryStatsData = networkStatsManager.QuerySummaryForDevice(ConnectivityType.Mobile, fetchSubId.SubId,
			dayBeforeTimestamp, currTime);
		var queryStatsWifi =
			networkStatsManager.QuerySummaryForDevice(ConnectivityType.Wifi, fetchSubId.SubId, dayBeforeTimestamp,
				currTime);
		if (queryStatsData == null || queryStatsWifi == null)
			throw new Exception("Network query is null!");

		var activeApps = usageQueryStatsResult.Where(s => s.TotalTimeInForeground > 0);
		var activeAppsLogMessage = string.Join('\n', activeApps.OrderByDescending(x => x.TotalTimeInForeground)
			.Select(x => $"{x.PackageName} : {TimeSpan.FromMilliseconds(x.TotalTimeInForeground)}"));
		GlobalContext.Logger.Info<ScheduledTaskReceiver>($"Device Usage Info:\n{activeAppsLogMessage}");

		long totalScreenTimeMs = activeApps
			.Sum(s => s.TotalTimeInForeground);

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

	private async Task CreateUnsustainableNotification(DeviceInteractionTrigger triggerType)
	{
		var promptRepo = GlobalContext.Services.GetRequiredService<PromptRepository>();
		var geminiSettings = GlobalContext.Services.GetKeyedService<GeminiSettings>(GlobalContext.GeminiNotification);
		var geminiClient = GlobalContext.Services.GetRequiredService<GeminiChat>();
		string notificationPrompt = await promptRepo.GetPrompt(triggerType);
		try
		{
			GlobalContext.Logger.Info<ScheduledTaskReceiver>($"Executing {triggerType} notification from daily activity logger.");
			await Utils.RetryAsyncTaskOrThrow<TaskCanceledException>(3, async () =>
			{
				var tunedNotification = await geminiClient.SendMessage(notificationPrompt, settings: geminiSettings);
				var deserializedStructuredMsg =
					JsonSerializer.Deserialize<List<TunedNotificationInfo>>(tunedNotification.Text!)!;
				var tunedNotificationInfoFirstEntry = deserializedStructuredMsg.First();
				NotificationSvc.SendInteractiveNotification(tunedNotificationInfoFirstEntry.NotificationTitle,
					tunedNotificationInfoFirstEntry.NotificationDescription, tunedNotificationInfoFirstEntry.FullContent);
			});
		}
		catch (Exception ex)
		{
			GlobalContext.Logger.Error<ScheduledTaskReceiver>(ex, "Daily activity notification resulted into an error.");
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
