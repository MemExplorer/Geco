using System.Text.Json;
using Android.App;
using Android.App.Usage;
using Android.Content;
using Android.Net;
using Android.Telephony;
using Geco.Core;
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
		GlobalContext.Logger.Info<ScheduledTaskReceiver>("Running weekly report...");
		var constructedPrompt = await ConstructLikelihoodPrompt();
		if (constructedPrompt == null)
			return;

		string likelihoodPrompt = constructedPrompt.GetValueOrDefault().Item1;
		double likelihoodProbability = constructedPrompt.GetValueOrDefault().Item2;
		string statusIcon = constructedPrompt.GetValueOrDefault().Item3;
		GlobalContext.Logger.Info<ScheduledTaskReceiver>("Created weekly report likelihood prompt.");
		try
		{
			GlobalContext.Logger.Info<ScheduledTaskReceiver>("Executing Scheduled Weekly Summary Notification.");
			var geminiClient = GlobalContext.Services.GetRequiredService<GeminiChat>();
			var geminiSettings =
				GlobalContext.Services.GetKeyedService<GeminiSettings>(GlobalContext.GeminiWeeklyReport);
			await Utils.RetryAsyncTaskOrThrow<TaskCanceledException>(3, async () =>
			{
				var weeklyReportResponse = await geminiClient.SendMessage(likelihoodPrompt, settings: geminiSettings);
				var deserializedWeeklyReport =
					JsonSerializer.Deserialize<List<WeeklyReportContent>>(weeklyReportResponse.Text!)!;
				var firstItem = deserializedWeeklyReport.First();

				// create chat history
				var chatRepo = GlobalContext.Services.GetRequiredService<ChatRepository>();
				string weeklyReportMessage = BuildWeeklyReport(firstItem, statusIcon, likelihoodProbability);
				var chatMsg = new ChatMessage(new ChatRole("model"), weeklyReportMessage);
				chatMsg.AdditionalProperties = new AdditionalPropertiesDictionary { ["id"] = (ulong)0 };
				string chatTitle = firstItem.NotificationTitle;
				var historyInstance = new GecoConversation(Guid.NewGuid().ToString(),
					HistoryType.WeeklyReportConversation, chatTitle,
					DateTimeOffset.UtcNow.ToUnixTimeSeconds(), [chatMsg], firstItem.NotificationDescription,
					weeklyReportMessage);
				await chatRepo.AppendHistory(historyInstance);
				NotificationSvc.SendInteractiveWeeklyReportNotification(historyInstance.Id, firstItem.NotificationTitle,
					firstItem.NotificationDescription,
					weeklyReportMessage);
			});
		}
		catch (Exception ex)
		{
			GlobalContext.Logger.Error<ScheduledTaskReceiver>(ex,
				"Weekly summary notification resulted into an error.");
		}
	}

	private string BuildWeeklyReport(WeeklyReportContent content, string status, double percentage) =>
		$$"""
		  <html>
		  	<head>
		  		<style>
		  		body {
		  			font-family: Arial, sans-serif;
		  			display: flex;
		  			flex-direction: column;
		  			align-items: center;
		  			padding: 20px;
		  			margin: 0;
		  		}
		  
		  		.circle {
		  			position: relative;
		  			width: 100px;
		  			height: 100px;
		  			border-radius: 50%;
		  			background: conic-gradient(var(--color, red) 0%, var(--color, red) var(--percentage, 0%), #ccc var(--percentage, 0%));
		  			display: flex;
		  			justify-content: center;
		  			align-items: center;
		  			box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
		  			margin-right: 20px;
		  		}
		  
		  		.circle::before {
		  			content: '';
		  			position: absolute;
		  			width: 80px;
		  			height: 80px;
		  			background-color: #f4f4f9;
		  			border-radius: 50%;
		  		}
		  
		  		.circle .grid-inner {
		  			position: absolute;
		  			color: #333;
		  			align-items:center;
		  			justify-content:center;
		  			display:flex;
		  		}
		  		
		  		.circle .grid-inner svg {
		  			padding-bottom: 2px;
		  		}
		  
		  		.overview {
		  			margin-bottom: 20px;
		  		}
		  
		  		.collapsible {
		  			max-width: 150px;
		  			background-color: #039967;
		  			color: white;
		  			border: none;
		  			outline: none;
		  			align-self: start;
		  			padding: 7px 10px;
		  			cursor: pointer;
		  			border-radius: 5px;
		  			box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
		  		}
		  
		  		.collapsible:hover {
		  			background-color: #026343;
		  		}
		  
		  		.content {
		  			margin-top: 5px;
		  			display: none;
		  			overflow: hidden;
		  		}
		  		
		  		.flex-container {
		  			display: flex;
		  			flex-wrap:wrap;
		  		}
		  		.title {
		  			height: auto;
		  			align-self:center;
		  		}
		  		</style>
		  	</head>
		  	<body>
		  		<div class='flex-container'>
		  			<div class='circle' id='percentageCircle'>
		  			<div class='grid-inner'>
		  				<h4 id='percentageValue'></h4>
		  				{{status}}
		  			</div>
		  			</div>
		  			
		  			<div class='title'>
		  				<h3>Weekly<br>Sustainability<br>Likelihood Report</h3>
		  			</div>
		  		</div>
		  		
		  		<div class='overview'>
		  			{{content.Overview}}
		  		</div>
		  		<button class='collapsible'>Find out more</button>
		  		<div class='content' id='collapsibleContent'>
		  			{{content.ReportBreakdown}}
		  		</div>
		  		<script>
		  		const collapsible = document.querySelector('.collapsible');
		  		const collapsibleContent = document.querySelector('.content');
		  		collapsible.addEventListener('click', () => {
		  			const isExpanded = collapsibleContent.style.display === 'block';
		  			collapsibleContent.style.display = isExpanded ? 'none' : 'block';
		  			collapsibleContent.style.color = document.body.style.color;
		  			collapsible.remove();
		  		});
		  
		  		function calculateColor(percentage) {
		  			let red, green;
		  			if (percentage <= 50) {
		  			red = 255;
		  			green = Math.round(percentage * 5.1);
		  			} else {
		  			red = Math.round((100 - percentage) * 5.1);
		  			green = 255;
		  			}
		  			return `rgb(${red}, ${green}, 0)`;
		  		}
		  		const percentage = {{percentage}};
		  		const circle = document.getElementById('percentageCircle');
		  		const percentageValue = document.getElementById('percentageValue');
		  		circle.style.setProperty('--color', calculateColor(percentage));
		  		circle.style.setProperty('--percentage', `${percentage}%`);
		  		percentageValue.textContent = `${percentage}%`;
		  		</script>
		  	</body>
		  </html>
		  """;

	private async Task<(string, double, string)?> ConstructLikelihoodPrompt()
	{
		const string upArrowSvg = """
		                          <svg xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink' version='1.1' width='20' height='20' viewBox='1 1 256 256' xml:space='preserve'>
		                          <defs>
		                          </defs>
		                          <g style='stroke: none; stroke-width: 0; stroke-dasharray: none; stroke-linecap: butt; stroke-linejoin: miter; stroke-miterlimit: 10; fill: none; fill-rule: nonzero; opacity: 1;' transform='translate(1.4065934065934016 1.4065934065934016) scale(2.81 2.81)' >
		                          <path d='M 43.779 0.434 L 12.722 25.685 c -0.452 0.368 -0.714 0.92 -0.714 1.502 v 19.521 c 0 0.747 0.43 1.427 1.104 1.748 c 0.674 0.321 1.473 0.225 2.053 -0.246 L 45 23.951 l 29.836 24.258 c 0.579 0.471 1.378 0.567 2.053 0.246 c 0.674 -0.321 1.104 -1.001 1.104 -1.748 V 27.187 c 0 -0.582 -0.263 -1.134 -0.714 -1.502 L 46.221 0.434 C 45.51 -0.145 44.49 -0.145 43.779 0.434 z' style='stroke: none; stroke-width: 1; stroke-dasharray: none; stroke-linecap: butt; stroke-linejoin: miter; stroke-miterlimit: 10; fill: rgb(39,193,39); fill-rule: nonzero; opacity: 1;' transform=' matrix(1 0 0 1 0 0) ' stroke-linecap='round' />
		                          <path d='M 43.779 41.792 l -31.057 25.25 c -0.452 0.368 -0.714 0.919 -0.714 1.502 v 19.52 c 0 0.747 0.43 1.427 1.104 1.748 c 0.674 0.321 1.473 0.225 2.053 -0.246 L 45 65.308 l 29.836 24.258 c 0.579 0.471 1.378 0.567 2.053 0.246 c 0.674 -0.321 1.104 -1.001 1.104 -1.748 V 68.544 c 0 -0.583 -0.263 -1.134 -0.714 -1.502 l -31.057 -25.25 C 45.51 41.214 44.49 41.214 43.779 41.792 z' style='stroke: none; stroke-width: 1; stroke-dasharray: none; stroke-linecap: butt; stroke-linejoin: miter; stroke-miterlimit: 10; fill: rgb(39,193,39); fill-rule: nonzero; opacity: 1;' transform=' matrix(1 0 0 1 0 0) ' stroke-linecap='round' />
		                          </g>
		                          </svg>
		                          """;

		const string downArrowSvg = """
		                            <svg xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink' version='1.1' width='20' height='20' viewBox='0 0 256 256' xml:space='preserve'>
		                            <defs>
		                            </defs>
		                            <g style='stroke: none; stroke-width: 0; stroke-dasharray: none; stroke-linecap: butt; stroke-linejoin: miter; stroke-miterlimit: 10; fill: none; fill-rule: nonzero; opacity: 1;' transform='translate(1.4065934065934016 1.4065934065934016) scale(2.81 2.81)' >
		                            <path d='M 43.779 89.566 L 12.722 64.315 c -0.452 -0.368 -0.714 -0.92 -0.714 -1.502 V 43.293 c 0 -0.747 0.43 -1.427 1.104 -1.748 c 0.674 -0.321 1.473 -0.225 2.053 0.246 L 45 66.049 l 29.836 -24.258 c 0.579 -0.471 1.378 -0.567 2.053 -0.246 c 0.674 0.321 1.104 1.001 1.104 1.748 v 19.521 c 0 0.582 -0.263 1.134 -0.714 1.502 L 46.221 89.566 C 45.51 90.145 44.49 90.145 43.779 89.566 z' style='stroke: none; stroke-width: 1; stroke-dasharray: none; stroke-linecap: butt; stroke-linejoin: miter; stroke-miterlimit: 10; fill: rgb(206,62,62); fill-rule: nonzero; opacity: 1;' transform=' matrix(1 0 0 1 0 0) ' stroke-linecap='round' />
		                            <path d='M 43.779 48.208 l -31.057 -25.25 c -0.452 -0.368 -0.714 -0.919 -0.714 -1.502 V 1.936 c 0 -0.747 0.43 -1.427 1.104 -1.748 c 0.674 -0.321 1.473 -0.225 2.053 0.246 L 45 24.692 L 74.836 0.434 c 0.579 -0.471 1.378 -0.567 2.053 -0.246 c 0.674 0.321 1.104 1.001 1.104 1.748 v 19.521 c 0 0.583 -0.263 1.134 -0.714 1.502 l -31.057 25.25 C 45.51 48.786 44.49 48.786 43.779 48.208 z' style='stroke: none; stroke-width: 1; stroke-dasharray: none; stroke-linecap: butt; stroke-linejoin: miter; stroke-miterlimit: 10; fill: rgb(206,62,62); fill-rule: nonzero; opacity: 1;' transform=' matrix(1 0 0 1 0 0) ' stroke-linecap='round' />
		                            </g>
		                            </svg>
		                            """;

		const string tildeSvg = """
		                        <svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 100 50' width='20' height='50'>
		                        	<path d='M1 25 Q 25 5, 50 25 T 90 25' 
		                        			fill='none' 
		                        			stroke='gray' 
		                        			stroke-width='15' />
		                        </svg>
		                        """;

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
				string statusIcon =
					currentWeekResult.Value.Probability.CompareTo(lastWeekResult.Value.Probability) switch
					{
						-1 => downArrowSvg,
						1 => upArrowSvg,
						_ => tildeSvg
					};

				// build likelihood prompt
				return (await promptRepo.GetLikelihoodWithHistoryPrompt(
					currentWeekResult.Value.Probability, currentWeekResult.Value.PositiveComputation,
					currentWeekResult.Value.Frequency,
					lastWeekResult.Value.Probability, lastWeekResult.Value.PositiveComputation,
					lastWeekResult.Value.Frequency), currentWeekResult.Value.Probability, statusIcon);
			}
		}

		return (await promptRepo.GetLikelihoodPrompt(currentWeekResult.Value.Probability,
			currentWeekResult.Value.PositiveComputation,
			currentWeekResult.Value.Frequency), currentWeekResult.Value.Probability, "");
	}

	private (string PositiveComputation, string Frequency, double Probability)? GetLikelihoodPromptFromRecords(
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
		double currSustainableProportionalProbability = Math.Round(currWeekComputationResult.PositiveProbs, 2);

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
