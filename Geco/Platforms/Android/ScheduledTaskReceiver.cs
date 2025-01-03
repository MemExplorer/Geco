using System.Globalization;
using System.Text.Json;
using Android.App;
using Android.App.Usage;
using Android.Content;
using Android.Net;
using Android.Net.Wifi.P2p;
using Android.Telephony;
using CommunityToolkit.Maui.Alerts;
using Geco.Core;
using Geco.Core.Database;
using Geco.Core.Models.ActionObserver;
using Geco.Core.Models.Notification;
using GoogleGeminiSDK;
using GoogleGeminiSDK.Models.Components;
using AndroidOS = Android.OS;

namespace Geco;

[BroadcastReceiver(Exported = true, Enabled = true)]
[IntentFilter(["com.ssbois.geco.ScheduledTaskReceiver"])]
internal class ScheduledTaskReceiver : BroadcastReceiver
{
	static IServiceProvider SvcProvider { get; }
	static INotificationManagerService NotificationSvc { get; }

	static ScheduledTaskReceiver()
	{
		SvcProvider = App.Current?.Handler.MauiContext?.Services!;
		NotificationSvc = SvcProvider.GetService<INotificationManagerService>()!;
	}

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
		catch
		{
			// do nothing
		}
	}

	private async Task RunWeeklySummaryNotification()
	{
		var triggerRepo = SvcProvider.GetService<TriggerRepository>();
		if (triggerRepo == null)
			throw new Exception("TriggerRepository should not be null!");

		var promptRepo = SvcProvider.GetService<PromptRepository>();
		if (promptRepo == null)
			throw new Exception("PromptRepository should not be null!");

		string? likelihoodPrompt = await ConstructLikelihoodPrompt(triggerRepo, promptRepo);
		if (likelihoodPrompt == null)
			return;

		var geminiClient = new GeminiChat(GecoSecrets.GEMINI_API_KEY, "gemini-1.5-flash-latest");
		var geminiSettings = new GeminiSettings
		{
			Conversational = false,
			ResponseMimeType = "application/json",
			ResponseSchema = new Schema(
			SchemaType.ARRAY,
			Items: new Schema(SchemaType.OBJECT,
				Properties: new Dictionary<string, Schema>
				{
									{ "NotificationDescription", new Schema(SchemaType.STRING) },
									{ "Content", new Schema(SchemaType.STRING) }
				},
				Required: ["NotificationDescription", "Content"]
			)
		)
		};

		try
		{
			var weeklyReportResponse = await geminiClient.SendMessage(likelihoodPrompt, settings: geminiSettings);
			var deserializedWeeklyReport = JsonSerializer.Deserialize<List<WeeklyReportContent>> (weeklyReportResponse.Text!)!;
			var firstItem = deserializedWeeklyReport.First();
			NotificationSvc.SendInteractiveNotification("GECO Weekly Report", firstItem.NotificationDescription, firstItem.Content);
		}
		catch (Exception ex)
		{
			await Toast.Make(ex.ToString()).Show();
		}
	}

	private async Task<string?> ConstructLikelihoodPrompt(TriggerRepository triggerRepo, PromptRepository promptRepo)
	{
		// fetch trigger records for current week
		var currentWeekTriggerRecords = (await triggerRepo.FetchWeekOneTriggerRecords()).ToDictionary();
		var currentWeekResult = GetLikelihoodPromptFromRecords(currentWeekTriggerRecords);
		if (currentWeekResult == null)
			return null;

		// check if we have data from last 2 weeks
		if (await triggerRepo.HasHistory() && false)
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

		return await promptRepo.GetLikelihoodPrompt(currentWeekResult.Value.Probability, currentWeekResult.Value.PositiveComputation,
					currentWeekResult.Value.Frequency);
	}

	private (string PositiveComputation, string Frequency, string Probability)? GetLikelihoodPromptFromRecords(Dictionary<DeviceInteractionTrigger, int> currentWeekTriggerRecords)
	{
		var chargingPositive = currentWeekTriggerRecords.GetValueOrDefault(DeviceInteractionTrigger.ChargingSustainable, 5);
		var chargingNegative = currentWeekTriggerRecords.GetValueOrDefault(DeviceInteractionTrigger.ChargingUnsustainable, 3);
		var networkUsagePositive = currentWeekTriggerRecords.GetValueOrDefault(DeviceInteractionTrigger.NetworkUsageSustainable, 6);
		var networkUsageNegative = currentWeekTriggerRecords.GetValueOrDefault(DeviceInteractionTrigger.NetworkUsageUnsustainable, 8);
		var deviceUsagePositive = currentWeekTriggerRecords.GetValueOrDefault(DeviceInteractionTrigger.DeviceUsageSustainable, 2);
		var deviceUsageNegative = currentWeekTriggerRecords.GetValueOrDefault(DeviceInteractionTrigger.DeviceUsageUnsustainable, 4);

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

		return (currWeekComputationStr.PositiveComputation, currWeekFrequencyStr, currSustainableProportionalProbability);
	}

	private async Task RunDeviceUsageLogger()
	{
		var svcProvider = App.Current?.Handler.MauiContext?.Services!;
		var triggerRepo = svcProvider.GetService<TriggerRepository>();
		if (triggerRepo == null)
			throw new Exception("TriggerRepository should not be null!");

		var networkStatsManager = (NetworkStatsManager?)Platform.AppContext.GetSystemService("netstats");
		var usageStatsManager = (UsageStatsManager?)Platform.AppContext.GetSystemService("usagestats");
		if (usageStatsManager == null || networkStatsManager == null)
			throw new Exception("One of the services are null");

		var fetchSubId = await GetSubscriptionId();
		long currTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
		long dayBeforeTimestamp = currTime - 86_400_000; // subtract current time by 1 day in ms
		var usageQueryStatsResult =
			usageStatsManager?.QueryUsageStats(UsageStatsInterval.Daily, dayBeforeTimestamp, currTime);
		if (usageQueryStatsResult == null)
			throw new Exception("Usage stats query is null");

		var queryStatsData = networkStatsManager.QuerySummaryForDevice(ConnectivityType.Mobile, fetchSubId.SubId,
			dayBeforeTimestamp, currTime);
		var queryStatsWifi =
			networkStatsManager.QuerySummaryForDevice(ConnectivityType.Wifi, fetchSubId.SubId, dayBeforeTimestamp,
				currTime);
		if (queryStatsData == null || queryStatsWifi == null)
			throw new Exception("Network query is null!");

		long totalScreenTimeMs = usageQueryStatsResult.Where(s => s.TotalTimeInForeground > 0)
			.Sum(s => s.TotalTimeInForeground);

		// check if screen time is equal or greater than 7 hrs in ms
		if (totalScreenTimeMs >= 25_200_000)
		{
			await triggerRepo.LogTrigger(DeviceInteractionTrigger.DeviceUsageUnsustainable, 1);
			await CreateUnsustainableNotification(SvcProvider, DeviceInteractionTrigger.DeviceUsageUnsustainable);
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
			await CreateUnsustainableNotification(SvcProvider, DeviceInteractionTrigger.BrowserUsageUnsustainable);
	}

	private async Task CreateUnsustainableNotification(IServiceProvider svcProvider, DeviceInteractionTrigger triggerType)
	{
		var promptRepo = svcProvider.GetService<PromptRepository>();
		if (promptRepo == null)
			throw new Exception("PromptRepository should not be null!");

		var geminiClient = new GeminiChat(GecoSecrets.GEMINI_API_KEY, "gemini-1.5-flash-latest");
		var geminiSettings = new GeminiSettings
		{
			Conversational = false,
			ResponseMimeType = "application/json",
			ResponseSchema = new Schema(
			SchemaType.ARRAY,
			Items: new Schema(SchemaType.OBJECT,
				Properties: new Dictionary<string, Schema>
				{
						{ "NotificationTitle", new Schema(SchemaType.STRING) },
						{ "NotificationDescription", new Schema(SchemaType.STRING) }
				},
				Required: ["NotificationTitle", "NotificationDescription"]
			)
		)
		};

		string notificationPrompt = await promptRepo.GetPrompt(triggerType);
		try
		{
			var tunedNotification = await geminiClient.SendMessage(notificationPrompt, settings: geminiSettings);
			var deserializedStructuredMsg =
				JsonSerializer.Deserialize<List<TunedNotificationInfo>>(tunedNotification.Text!)!;
			var tunedNotificationInfoFirstEntry = deserializedStructuredMsg.First();
			(string Title, string Description) notificationInfo = (tunedNotificationInfoFirstEntry.NotificationTitle,
				tunedNotificationInfoFirstEntry.NotificationDescription);
			NotificationSvc.SendInteractiveNotification(notificationInfo.Title, notificationInfo.Description);
		}
		catch (Exception ex)
		{
			await Toast.Make(ex.ToString()).Show();
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
