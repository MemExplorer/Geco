using System.Globalization;
using System.Text.Json;
using Android.App;
using Android.App.Usage;
using Android.Content;
using Android.Net;
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

		// fetch trigger records for current week
		var currentWeekTriggerRecords = await triggerRepo.FetchWeekOneTriggerRecords();

		// pass the first week's data to Bayes Math Model
		var currWeekBayesInst = new BayesTheorem();
		currWeekBayesInst.AppendData("Charging",
			currentWeekTriggerRecords[DeviceInteractionTrigger.ChargingSustainable],
			currentWeekTriggerRecords[DeviceInteractionTrigger.ChargingUnsustainable]);
		currWeekBayesInst.AppendData("Network Usage",
			currentWeekTriggerRecords[DeviceInteractionTrigger.NetworkUsageSustainable],
			currentWeekTriggerRecords[DeviceInteractionTrigger.NetworkUsageUnsustainable]);
		currWeekBayesInst.AppendData("Device Usage",
			currentWeekTriggerRecords[DeviceInteractionTrigger.DeviceUsageSustainable],
			currentWeekTriggerRecords[DeviceInteractionTrigger.DeviceUsageUnsustainable]);

		// gets values we need for prompt
		var currWeekComputationResult = currWeekBayesInst.Compute();
		var currWeekComputationStr = currWeekBayesInst.GetComputationInString();
		string currWeekFrequencyStr = currWeekBayesInst.GetFrequencyInString();
		string currSustainableProportionalProbability = Math.Round(currWeekComputationResult.PositiveProbs, 2)
			.ToString(CultureInfo.InvariantCulture) + "%";

		// check if we have data from last 2 weeks
		string likelihoodPrompt;
		if (await triggerRepo.HasHistory())
		{
			// fetch data from last 2 weeks
			var lastWeekTriggerRecords = await triggerRepo.FetchWeekTwoTriggerRecords();
			var lastWeekBayesInst = new BayesTheorem();
			lastWeekBayesInst.AppendData("Charging",
				lastWeekTriggerRecords[DeviceInteractionTrigger.ChargingSustainable],
				lastWeekTriggerRecords[DeviceInteractionTrigger.ChargingUnsustainable]);
			lastWeekBayesInst.AppendData("Network Usage",
				lastWeekTriggerRecords[DeviceInteractionTrigger.NetworkUsageSustainable],
				lastWeekTriggerRecords[DeviceInteractionTrigger.NetworkUsageUnsustainable]);
			lastWeekBayesInst.AppendData("Device Usage",
				lastWeekTriggerRecords[DeviceInteractionTrigger.DeviceUsageSustainable],
				lastWeekTriggerRecords[DeviceInteractionTrigger.DeviceUsageUnsustainable]);

			// get the values we need to construct the prompt
			var lastWeekComputationResult = lastWeekBayesInst.Compute();
			var lastWeekComputationStr = lastWeekBayesInst.GetComputationInString();
			string lastWeekFrequencyStr = lastWeekBayesInst.GetFrequencyInString();
			string lastSustainableProportionalProbability = Math.Round(lastWeekComputationResult.PositiveProbs, 2)
				.ToString(CultureInfo.InvariantCulture) + "%";

			// build likelihood prompt
			likelihoodPrompt = await promptRepo.GetLikelihoodWithHistoryPrompt(
				currSustainableProportionalProbability, currWeekComputationStr.PositiveComputation,
				currWeekFrequencyStr,
				lastSustainableProportionalProbability, lastWeekComputationStr.PositiveComputation,
				lastWeekFrequencyStr);
		}
		else
		{
			// build likelihood prompt
			likelihoodPrompt =
				await promptRepo.GetLikelihoodPrompt(currSustainableProportionalProbability,
					currWeekComputationStr.PositiveComputation, currWeekFrequencyStr);
		}

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

		try
		{
			var weeklyReportResponse = await geminiClient.SendMessage(likelihoodPrompt, settings: geminiSettings);
			var deserializedWeeklyReport = JsonSerializer.Deserialize<IDictionary<string, string>>(weeklyReportResponse.Text!)!;
			string notificationDesc = deserializedWeeklyReport["NotificationDescription"];
			string notificationContent = deserializedWeeklyReport["Content"];
			NotificationSvc.SendInteractiveNotification("GECO Weekly Report", notificationDesc, notificationContent);
		}
		catch (Exception ex)
		{
			await Toast.Make(ex.ToString()).Show();
		}
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
