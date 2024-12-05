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
	const int ServiceId = 1000;
	bool _monitoring = false;

	private IServiceProvider SvcProvider { get; }
	private INotificationManagerService NotificationSvc { get; }
	private IDeviceStateObserver[] Observers { get; }

	public DeviceUsageMonitorService()
	{
		SvcProvider = App.Current?.Handler.MauiContext?.Services!;
		NotificationSvc = SvcProvider.GetService<INotificationManagerService>()!;
		Observers = [.. SvcProvider.GetServices<IDeviceStateObserver>()];
	}
	
	public override StartCommandResult OnStartCommand(Intent? intent, [GeneratedEnum] StartCommandFlags flags, int startId)
	{
		if (intent?.Action == "START_SERVICE" && !_monitoring)
		{
			_monitoring = true;
			if (NotificationSvc is not NotificationManagerService nms) 
				return StartCommandResult.Sticky;
				
			var notification = nms.Show("Monitoring Mobile Actions", "Geco is currently monitoring your mobile actions in the background");
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

		}
		else if (intent?.Action == "STOP_SERVICE" && _monitoring)
		{
			_monitoring = false;
			if (OperatingSystem.IsAndroidVersionAtLeast(33))
				StopForeground(StopForegroundFlags.Remove);
			else
				StopForeground(true);

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
		var startService = new Intent(Platform.AppContext, this.Class);
		startService.SetAction("START_SERVICE");
		Platform.CurrentActivity?.StartService(startService);
	}

	public void Stop()
	{
		var stopIntent = new Intent(Platform.AppContext, this.Class);
		stopIntent.SetAction("STOP_SERVICE");
		Platform.CurrentActivity?.StartService(stopIntent);
	}

	private void OnDeviceStateChanged(object? sender, TriggerEventArgs e)
	{
		if (e.TriggerType == DeviceInteractionTrigger.ChargingUnsustainable)
		{
			// Temporary Notification to test trigger
			NotificationSvc.SendNotification("Unsustainable Charging", "Charging range outside sustainable range of 20-80%");
		}
		else if (e.TriggerType == DeviceInteractionTrigger.NetworkUsageUnsustainable)
		{
			// Temporary Notification to test trigger
			NotificationSvc.SendNotification("Unsustainable Network", "Please use wifi instead of cellular data.");
		}
	}

	public override IBinder? OnBind(Intent? intent) => null;
}
