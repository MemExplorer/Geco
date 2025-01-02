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
using GoogleGeminiSDK.Models.Components;

namespace Geco;

[Service(Name = "com.ssbois.geco.DeviceUsageMonitorService")]
public class DeviceUsageMonitorService : Service, IPlatformActionObserver
{
	public const int TaskScheduleId = 84268154;
	const int ServiceId = 1000;
	static bool _hasStarted;

	private IServiceProvider SvcProvider { get; }
	private INotificationManagerService NotificationSvc { get; }
	private IDeviceStateObserver[] Observers { get; }
	private GeminiChat GeminiChat { get; }
	private GeminiSettings GeminiSettings { get; }

	public DeviceUsageMonitorService()
	{
		SvcProvider = App.Current?.Handler.MauiContext?.Services!;
		NotificationSvc = SvcProvider.GetService<INotificationManagerService>()!;
		Observers = [.. SvcProvider.GetServices<IDeviceStateObserver>()];
		GeminiChat = new GeminiChat(GecoSecrets.GEMINI_API_KEY, "gemini-1.5-flash-latest");
		GeminiSettings = new GeminiSettings
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
		var nextWeek = DateTime.Today.AddDays(7).AddHours(6);
		InternalCreateScheduledTask("weektasksummarycmd", nextWeek);
	}

	private void CancelScheduledWeeklySummary() =>
		InternalCancelScheduledTask("weektasksummarycmd");

	private void CancelDeviceUsageScheduledLogger() =>
		InternalCancelScheduledTask("schedtaskcmd");

	public static void CreateDeviceUsageScheduledLogger() =>
		InternalCreateScheduledTask("schedtaskcmd", DateTime.Now.Date.AddDays(1));

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
			var triggerRepo = SvcProvider.GetService<TriggerRepository>();
			if (triggerRepo == null)
				throw new Exception("TriggerRepository should not be null!");

			var promptRepo = SvcProvider.GetService<PromptRepository>();
			if (promptRepo == null)
				throw new Exception("PromptRepository should not be null!");

			// don't notify if trigger is in cooldown
			if (await triggerRepo.IsTriggerInCooldown(e.TriggerType))
				return;

			// ensure that we are only creating notifications for unsustainable trigger types
			(string Title, string Description) notificationInfo = (string.Empty, string.Empty);
			bool requestCompleted = false;
			if (e.TriggerType < 0)
			{
				string notificationPrompt = await promptRepo.GetPrompt(e.TriggerType);
				try
				{
					var tunedNotification = await GeminiChat.SendMessage(notificationPrompt, settings: GeminiSettings);
					var deserializedStructuredMsg =
						JsonSerializer.Deserialize<List<TunedNotificationInfo>>(tunedNotification.Text!)!;
					var tunedNotificationInfoFirstEntry = deserializedStructuredMsg.First();
					notificationInfo = (tunedNotificationInfoFirstEntry.NotificationTitle,
						tunedNotificationInfoFirstEntry.NotificationDescription);
					requestCompleted = true;
				}
				catch (Exception ex)
				{
					await Toast.Make(ex.ToString()).Show();
				}
			}

			switch (e.TriggerType)
			{
			// only record charging
			case DeviceInteractionTrigger.ChargingUnsustainable:

				await triggerRepo.LogTrigger(e.TriggerType, 1);
				if (requestCompleted)
					NotificationSvc.SendInteractiveNotification(notificationInfo.Title, notificationInfo.Description);
				break;
			case DeviceInteractionTrigger.ChargingSustainable:
				await triggerRepo.LogTrigger(e.TriggerType, 1);
				break;
			// don't count the triggers below
			case DeviceInteractionTrigger.NetworkUsageUnsustainable:
				await triggerRepo.LogTrigger(e.TriggerType, 0);
				if (requestCompleted)
					NotificationSvc.SendInteractiveNotification(notificationInfo.Title, notificationInfo.Description);
				break;
			case DeviceInteractionTrigger.LocationUsageUnsustainable:
				await triggerRepo.LogTrigger(e.TriggerType, 0);
				if (requestCompleted)
					NotificationSvc.SendInteractiveNotification(notificationInfo.Title, notificationInfo.Description);
				break;
			}
		}
		catch
		{
			// do nothing
		}
	}

	public override IBinder? OnBind(Intent? intent) => null;
}
