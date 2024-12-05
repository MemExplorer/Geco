using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Geco.Core.Database;
using Geco.Models.Monitor;
using Geco.Models.Notifications;

namespace Geco;

[Service(Name = "com.ssbois.geco.DeviceUsageMonitorService")]
public class DeviceUsageMonitorService : Service, IMonitorManagerService
{
	int _serviceId = 1000;
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

	[return: GeneratedEnum]
	public override StartCommandResult OnStartCommand(Intent? intent, [GeneratedEnum] StartCommandFlags flags, int startId)
	{
		if (intent?.Action == "START_SERVICE")
		{
			if (!_monitoring)
			{
				_monitoring = true;
				if (NotificationSvc is NotificationManagerService nms)
				{
					var notification = nms.Show("Monitoring Mobile Actions", "Geco is currenly monitoring your mobile actions in the background");
					notification.Flags = NotificationFlags.OngoingEvent;
					if (OperatingSystem.IsAndroidVersionAtLeast(29))
						StartForeground(_serviceId, notification, Android.Content.PM.ForegroundService.TypeDataSync);
					else
						StartForeground(_serviceId, notification);

					// start listening to device change events
					foreach (var observer in Observers)
					{
						observer.OnStateChanged += OnDeviceStateChanged;
						observer.StartEventListener();
					}
				}
			}

		}
		else if (intent?.Action == "STOP_SERVICE")
		{
			if (_monitoring)
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
		}

		return StartCommandResult.Sticky;
	}


	public void Start()
	{
		Intent startService = new Intent(Platform.AppContext, this.Class);
		startService.SetAction("START_SERVICE");
		Platform.CurrentActivity?.StartService(startService);
	}

	public void Stop()
	{
		Intent stopIntent = new Intent(Platform.AppContext, this.Class);
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
	}

	public override IBinder? OnBind(Intent? intent) => null;
}
