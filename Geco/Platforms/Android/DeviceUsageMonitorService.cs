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
	private INotificationManagerService NotificationSvc { get; } = App.Current?.Handler.MauiContext?.Services.GetService<INotificationManagerService>()!;
	private TriggerRepository triggerRepos { get; } = App.Current?.Handler.MauiContext?.Services.GetService<TriggerRepository>()!;

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

					StartMonitoringActions();
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

				StopMonitoringActions();
				StopSelfResult(startId);
			}
		}

		return StartCommandResult.Sticky;
	}


	private void StartMonitoringActions()
	{
		CheckBatteryStatus();
		Battery.Default.BatteryInfoChanged += OnBatteryInfoChanged;

		//Add more action monitoring here

	}

	private void StopMonitoringActions()
	{
		Battery.Default.BatteryInfoChanged -= OnBatteryInfoChanged;
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

	private void OnBatteryInfoChanged(object? sender, BatteryInfoChangedEventArgs e)
	{
		CheckBatteryStatus();
	}

	private async void CheckBatteryStatus()
	{
		var batteryInfo = Battery.Default.ChargeLevel;
		var chargeLevel = batteryInfo * 100;
		bool isCharging = Battery.Default.State == BatteryState.Charging;
		bool inCooldown = await triggerRepos.IsTriggerInCooldown(DeviceInteractionTrigger.ChargingUnsustainable);

		// Check if the battery percentage when charging is outside the range of 20-80%
		if (!inCooldown && isCharging && Battery.Default.PowerSource != BatteryPowerSource.Battery && (chargeLevel < 20 || chargeLevel > 80))
		{
			// Temporary Notification to test trigger
			NotificationSvc.SendNotification("Unsustainable Charging", "Charging range outside sustainable range of 20-80%");

			//Store the action trigger in the database
			await triggerRepos.LogTrigger(DeviceInteractionTrigger.ChargingUnsustainable, 1);
		}
	}

	public override IBinder? OnBind(Intent? intent) => null;
}
