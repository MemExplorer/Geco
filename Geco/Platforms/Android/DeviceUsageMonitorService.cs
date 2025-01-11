using System.Text.Json;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using AndroidX.Core.Content;
using CommunityToolkit.Maui.Alerts;
using Geco.Core.Database;
using Geco.Core.Models.ActionObserver;
using Geco.Core.Models.Notification;
using Geco.Notifications;
using GoogleGeminiSDK;

namespace Geco;

[Service(Name = "com.ssbois.geco.DeviceUsageMonitorService")]
public class DeviceUsageMonitorService : Service, IPlatformActionObserver
{
	public const int TaskScheduleId = 84268154;
	const int ServiceId = 1000;
	static bool _hasStarted;

	private INotificationManagerService NotificationSvc { get; }
	private IDeviceStateObserver[] Observers { get; }

	public DeviceUsageMonitorService()
	{
		NotificationSvc = GlobalContext.Services.GetRequiredService<INotificationManagerService>()!;
		Observers = [.. GlobalContext.Services.GetServices<IDeviceStateObserver>()];
	}

	public override StartCommandResult OnStartCommand(Intent? intent, [GeneratedEnum] StartCommandFlags flags,
		int startId)
	{
		if (intent?.Action == "START_SERVICE" && _hasStarted)
		{
			
			if (NotificationSvc is not NotificationManagerService nms)
				return StartCommandResult.Sticky;

			const string notificationDesc = "Geco is currently monitoring your mobile actions in the background";
			var notification = nms.SendPersistentNotification("Monitoring Mobile Actions", notificationDesc);
			if (OperatingSystem.IsAndroidVersionAtLeast(29))
				StartForeground(ServiceId, notification!, ForegroundService.TypeDataSync);
			else
				StartForeground(ServiceId, notification);

			// start listening to device change events
			foreach (var observer in Observers)
			{
				observer.OnStateChanged += OnDeviceStateChanged;
				observer.StartEventListener();
			}

			CreateDeviceUsageScheduledLogger();
			CreateScheduledWeeklySummary();
		}
		else if (intent?.Action == "STOP_SERVICE" && !_hasStarted)
		{
			if (OperatingSystem.IsAndroidVersionAtLeast(33))
				StopForeground(StopForegroundFlags.Remove);
			else
				StopForeground(true);

			CancelDeviceUsageScheduledLogger();
			CancelScheduledWeeklySummary();

			// stop listening to device change events
			foreach (var observer in Observers)
			{
				observer.OnStateChanged -= OnDeviceStateChanged;
				observer.StopEventListener();
			}

			StopSelfResult(startId);
		}

		return StartCommandResult.Sticky;
	}

	public void Start()
	{
		_hasStarted = true;
		var startService = new Intent(Platform.AppContext, Class);
		startService.SetAction("START_SERVICE");
		ContextCompat.StartForegroundService(Platform.AppContext, startService);
	}

	public void Stop()
	{
		_hasStarted = false;
		var stopIntent = new Intent(Platform.AppContext, Class);
		stopIntent.SetAction("STOP_SERVICE");
		Platform.AppContext.StopService(stopIntent);
	}

	public static void CreateScheduledWeeklySummary()
	{
		// Create weekly report every 6am
		DateTime nextWeek;
		if (DateTime.Now > GecoSettings.WeeklyReportDateTime.Subtract(new TimeSpan(0, 5, 0)))
		{
			nextWeek = DateTime.Today.AddDays(7).AddHours(6);
			GecoSettings.WeeklyReportDateTime = nextWeek;
		}
		else
			nextWeek = GecoSettings.WeeklyReportDateTime;

		InternalCreateScheduledTask("weektasksummarycmd", nextWeek);
	}

	private void CancelScheduledWeeklySummary() =>
		InternalCancelScheduledTask("weektasksummarycmd");

	private void CancelDeviceUsageScheduledLogger() =>
		InternalCancelScheduledTask("schedtaskcmd");

	public static void CreateDeviceUsageScheduledLogger()
	{
		DateTime nextDay;
		if(DateTime.Now > GecoSettings.DailyReportDateTime.Subtract(new TimeSpan(0, 5, 0)))
		{
			nextDay = DateTime.Today.AddDays(1);
			GecoSettings.DailyReportDateTime = nextDay;
		}	
		else
			nextDay = GecoSettings.DailyReportDateTime;

		InternalCreateScheduledTask("schedtaskcmd", nextDay);
	}	

	private static void InternalCancelScheduledTask(string action)
	{
		var intent = new Intent(Platform.AppContext, typeof(ScheduledTaskReceiver));
		intent.SetAction(action);
		intent.SetFlags(ActivityFlags.ReceiverForeground);
		var pendingIntent = PendingIntent.GetBroadcast(Platform.AppContext, TaskScheduleId, intent,
			PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
		if (pendingIntent == null)
			throw new Exception("pendingIntent is unexpectedly null");

		var alarmManager = (AlarmManager?)Platform.AppContext.GetSystemService(AlarmService);
		if (alarmManager == null)
			throw new Exception("alarmManager is unexpectedly null");

		alarmManager.Cancel(pendingIntent);
	}

	private static void InternalCreateScheduledTask(string action, DateTime scheduledDate)
	{
		var intent = new Intent(Platform.AppContext, typeof(ScheduledTaskReceiver));
		intent.SetAction(action);
		intent.SetFlags(ActivityFlags.ReceiverForeground);
		var pendingIntent = PendingIntent.GetBroadcast(Platform.AppContext, TaskScheduleId, intent,
			PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
		if (pendingIntent == null)
			throw new Exception("pendingIntent is unexpectedly null");

		var alarmManager = (AlarmManager?)Platform.AppContext.GetSystemService(AlarmService);
		if (alarmManager == null)
			throw new Exception("alarmManager is unexpectedly null");

		long triggerTimeInUnixTimeMs = ((DateTimeOffset)scheduledDate).ToUnixTimeMilliseconds();
		alarmManager.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, triggerTimeInUnixTimeMs, pendingIntent);
	}

	private async void OnDeviceStateChanged(object? sender, TriggerEventArgs e)
	{
		try
		{
			// don't notify if trigger is in cooldown
			var triggerRepo = GlobalContext.Services.GetRequiredService<TriggerRepository>();
			if (await triggerRepo.IsTriggerInCooldown(e.TriggerType))
				return;

			// Log trigger first
			switch (e.TriggerType)
			{
				case DeviceInteractionTrigger.ChargingUnsustainable:
				case DeviceInteractionTrigger.ChargingSustainable:
					await triggerRepo.LogTrigger(e.TriggerType, 1);
					break;
				// don't count the triggers below
				case DeviceInteractionTrigger.NetworkUsageUnsustainable:
				case DeviceInteractionTrigger.LocationUsageUnsustainable:
					await triggerRepo.LogTrigger(e.TriggerType, 0);
					break;
			}

			// ensure that we are only creating notifications for unsustainable trigger types
			if (e.TriggerType > 0)
				return;


			var promptRepo = GlobalContext.Services.GetRequiredService<PromptRepository>();
			var geminiSettings = GlobalContext.Services.GetKeyedService<GeminiSettings>(GlobalContext.GeminiNotification);
			var geminiChat = GlobalContext.Services.GetRequiredService<GeminiChat>();

			// create notification for the unsustainable trigger
			string notificationPrompt = await promptRepo.GetPrompt(e.TriggerType);
			try
			{
				var tunedNotification = await geminiChat.SendMessage(notificationPrompt, settings: geminiSettings);
				var deserializedStructuredMsg =
					JsonSerializer.Deserialize<List<TunedNotificationInfo>>(tunedNotification.Text!)!;
				var tunedNotificationInfoFirstEntry = deserializedStructuredMsg.First();
				NotificationSvc.SendInteractiveNotification(tunedNotificationInfoFirstEntry.NotificationTitle, tunedNotificationInfoFirstEntry.NotificationDescription, tunedNotificationInfoFirstEntry.FullContent);
			}
			catch (Exception geminiError)
			{
				GlobalContext.Logger.Error<DeviceUsageMonitorService>(geminiError);
			}
		}
		catch (Exception ex)
		{
			GlobalContext.Logger.Error<DeviceUsageMonitorService>(ex);
		}
	}

	public override IBinder? OnBind(Intent? intent) => null;
}
