using System.Text.Json;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Geco.Core.Database;
using Geco.Models.DeviceState;
using Geco.Models.Notifications;
using GoogleGeminiSDK;
using GoogleGeminiSDK.Models.Components;
using Microsoft.Maui.Controls.PlatformConfiguration;

namespace Geco;

[Service(Name = "com.ssbois.geco.DeviceUsageMonitorService")]
public class DeviceUsageMonitorService : Service, IMonitorManagerService
{
	public const int TaskScheduleId = 84268154;
	const int ServiceId = 1000;
	static bool _hasStarted = false;

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
		GeminiSettings = new GeminiSettings()
		{
			Conversational = false,
			ResponseMimeType = "application/json",
			ResponseSchema = new Schema(
				SchemaType.ARRAY,
				Items: new Schema(SchemaType.OBJECT,
					Properties: new Dictionary<string, Schema>
					{
						{"NotificationTitle", new Schema(SchemaType.STRING)},
						{"NotificationDescription", new Schema(SchemaType.STRING)}
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
				StartForeground(ServiceId, notification!, Android.Content.PM.ForegroundService.TypeDataSync);
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
		var startService = new Intent(Platform.AppContext, this.Class);
		startService.SetAction("START_SERVICE");
		Platform.CurrentActivity?.StartService(startService);
	}

	public void Stop()
	{
		_hasStarted = false;
		var stopIntent = new Intent(Platform.AppContext, this.Class);
		stopIntent.SetAction("STOP_SERVICE");
		Platform.CurrentActivity?.StartService(stopIntent);
	}

	public static void CreateScheduledWeeklySummary()
	{
		// https://stackoverflow.com/a/6346190
		var today = DateTime.Today;
		int daysUntilMonday = ((int) DayOfWeek.Monday - (int) today.DayOfWeek + 7) % 7;
		
		// Create weekly report every monday 6am
		var nextMonday = today.AddDays(daysUntilMonday).AddHours(6);
		InternalCreateScheduledTask("weektasksummarycmd", nextMonday);
	}
	
	public static void CancelScheduledWeeklySummary() =>
		InternalCancelScheduledTask("weektasksummarycmd");

	private void CancelDeviceUsageScheduledLogger() =>
		InternalCancelScheduledTask("schedtaskcmd");

	private void CreateDeviceUsageScheduledLogger() =>
		InternalCreateScheduledTask("schedtaskcmd", DateTime.Now.Date.AddDays(1));

	private static void InternalCancelScheduledTask(string action)
	{
		var intent = new Intent(Platform.AppContext, typeof(ScheduledTaskReceiver));
		intent.SetAction(action);
		intent.SetFlags(ActivityFlags.ReceiverForeground);
		var pendingIntent = PendingIntent.GetBroadcast(Platform.AppContext, TaskScheduleId, intent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
		if (pendingIntent == null)
			throw new Exception("pendingIntent is unexpectedly null");

		var alarmManager = (AlarmManager?)Platform.AppContext.GetSystemService(Context.AlarmService);
		if (alarmManager == null)
			throw new Exception("alarmManager is unexpectedly null");

		alarmManager.Cancel(pendingIntent);
	}

	private static void InternalCreateScheduledTask(string action, DateTime scheduledDate)
	{
		var intent = new Intent(Platform.AppContext, typeof(ScheduledTaskReceiver));
		intent.SetAction(action);
		intent.SetFlags(ActivityFlags.ReceiverForeground);
		var pendingIntent = PendingIntent.GetBroadcast(Platform.AppContext, TaskScheduleId, intent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
		if (pendingIntent == null)
			throw new Exception("pendingIntent is unexpectedly null");

		var alarmManager = (AlarmManager?)Platform.AppContext.GetSystemService(Context.AlarmService);
		if (alarmManager == null)
			throw new Exception("alarmManager is unexpectedly null");

		var triggerTimeInUnixTimeMs = ((DateTimeOffset)scheduledDate).ToUnixTimeMilliseconds();
		alarmManager.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, triggerTimeInUnixTimeMs, pendingIntent);
	}
	
	private async void OnDeviceStateChanged(object? sender, TriggerEventArgs e)
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
		if (e.TriggerType < 0)
		{
			string notificationPrompt = await promptRepo.GetPrompt(e.TriggerType);
			var tunedNotification = await GeminiChat.SendMessage(notificationPrompt, settings: GeminiSettings);
			var deserializedStructuredMsg = JsonSerializer.Deserialize<List<TunedNotificationInfo>>(tunedNotification.Text!)!;
			var tunedNotificationInfoFirstEntry = deserializedStructuredMsg.First();
			notificationInfo = (tunedNotificationInfoFirstEntry.NotificationTitle, tunedNotificationInfoFirstEntry.NotificationDescription);
		}

		switch (e.TriggerType)
		{
		// only record charging
		case DeviceInteractionTrigger.ChargingUnsustainable:

			await triggerRepo.LogTrigger(e.TriggerType, 1);
			NotificationSvc.SendInteractiveNotification(notificationInfo.Title,notificationInfo.Description);
			break;
		case DeviceInteractionTrigger.ChargingSustainable:
			await triggerRepo.LogTrigger(e.TriggerType, 1);
			break;

		// don't count the triggers below
		case DeviceInteractionTrigger.NetworkUsageUnsustainable:
			await triggerRepo.LogTrigger(e.TriggerType, 0);
			NotificationSvc.SendInteractiveNotification(notificationInfo.Title,notificationInfo.Description);
			break;
		case DeviceInteractionTrigger.LocationUsageUnsustainable:
			await triggerRepo.LogTrigger(e.TriggerType, 0);
			NotificationSvc.SendInteractiveNotification(notificationInfo.Title,notificationInfo.Description);
			break;

		}
	}

	public override IBinder? OnBind(Intent? intent) => null;
}

record TunedNotificationInfo(string NotificationTitle, string NotificationDescription);
