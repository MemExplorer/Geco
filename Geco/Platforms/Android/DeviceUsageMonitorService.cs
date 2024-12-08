using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Geco.Core.Database;
using Geco.Models.DeviceState;
using Geco.Models.Notifications;

namespace Geco;

[Service(Name = "com.ssbois.geco.DeviceUsageMonitorService")]
public class DeviceUsageMonitorService : Service, IMonitorManagerService
{
	public const int TaskScheduleId = 84268154;
	const int ServiceId = 1000;
	static bool hasStarted = false;

	private IServiceProvider SvcProvider { get; }
	private INotificationManagerService NotificationSvc { get; }
	private IDeviceStateObserver[] Observers { get; }

	public DeviceUsageMonitorService()
	{
		SvcProvider = App.Current?.Handler.MauiContext?.Services!;
		NotificationSvc = SvcProvider.GetService<INotificationManagerService>()!;
		Observers = [.. SvcProvider.GetServices<IDeviceStateObserver>()];
	}

	public override StartCommandResult OnStartCommand(Intent? intent, [GeneratedEnum] StartCommandFlags flags,
		int startId)
	{
		if (intent?.Action == "START_SERVICE" && hasStarted)
		{
			if (NotificationSvc is not NotificationManagerService nms)
				return StartCommandResult.Sticky;

			var notification = nms.Show("Monitoring Mobile Actions",
				"Geco is currently monitoring your mobile actions in the background");
			notification.Flags = NotificationFlags.OngoingEvent;
			if (OperatingSystem.IsAndroidVersionAtLeast(29))
				StartForeground(ServiceId, notification, Android.Content.PM.ForegroundService.TypeDataSync);
			else
				StartForeground(ServiceId, notification);

			// start listening to device change events
			foreach (var observer in Observers)
			{
				observer.OnStateChanged += OnDeviceStateChanged;
				observer.StartEventListener();
			}

			CreateDeviceUsageScheduledLogger();
		}
		else if (intent?.Action == "STOP_SERVICE" && !hasStarted)
		{
			if (OperatingSystem.IsAndroidVersionAtLeast(33))
				StopForeground(StopForegroundFlags.Remove);
			else
				StopForeground(true);

			CancelDeviceUsageScheduledLogger();

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
		hasStarted = true;
		var startService = new Intent(Platform.AppContext, this.Class);
		startService.SetAction("START_SERVICE");
		Platform.CurrentActivity?.StartService(startService);
	}

	public void Stop()
	{
		hasStarted = false;
		var stopIntent = new Intent(Platform.AppContext, this.Class);
		stopIntent.SetAction("STOP_SERVICE");
		Platform.CurrentActivity?.StartService(stopIntent);
	}

	private void CancelDeviceUsageScheduledLogger() =>
		InternalCancelScheduledTask("schedtaskcmd");

	private void CreateDeviceUsageScheduledLogger() =>
		InternalCreateScheduledTask("schedtaskcmd", DateTime.Now.Date.AddDays(1));

	private void InternalCancelScheduledTask(string action)
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

	private void InternalCreateScheduledTask(string action, DateTime scheduledDate)
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

	private void OnDeviceStateChanged(object? sender, TriggerEventArgs e)
	{
		if (e.TriggerType == DeviceInteractionTrigger.ChargingUnsustainable)
		{
			// Temporary Notification to test trigger
			NotificationSvc.SendNotification("Unsustainable Charging",
				"Charging range outside sustainable range of 20-80%");
		}
		else if (e.TriggerType == DeviceInteractionTrigger.NetworkUsageUnsustainable)
		{
			// Temporary Notification to test trigger
			NotificationSvc.SendNotification("Unsustainable Network", "Please use wifi instead of cellular data.");
		}
	}

	public override IBinder? OnBind(Intent? intent) => null;
}
