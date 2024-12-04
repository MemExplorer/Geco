using Android.App;
using Android.Content;
using Android.OS;
using Geco.Models.Monitor;
using Android.Runtime;

namespace Geco;

[Service]
public class DeviceUsageMonitorService: Service, IMonitorManagerService
{
	private readonly NotificationManagerService _notificationManagerService = new();
	int foregroundID = 1000;
	bool _isMonitoringEnabled = false;

	public override IBinder OnBind(Intent? intent)
	{
		throw new NotImplementedException();
	}

	[return: GeneratedEnum]
	public override StartCommandResult OnStartCommand(Intent? intent, [GeneratedEnum] StartCommandFlags flags, int startId)
	{
		if (intent?.Action == "START_SERVICE")
		{
			if (!_isMonitoringEnabled)
			{
				var notification = _notificationManagerService.Show("Monitoring Mobile Actions", "Geco is currenly monitoring your mobile actions in the background", true);

				StartForeground(foregroundID, notification);

				StartMonitoringActions();
			}

		}
		else if (intent?.Action == "STOP_SERVICE")
		{
			if (Build.VERSION.SdkInt < BuildVersionCodes.Tiramisu)
			{
#pragma warning disable CA1422
				StopForeground(true);
#pragma warning restore CA1422
			}
			StopMonitoringActions();
			StopSelfResult(startId);
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
#pragma warning disable CS8604
		Intent startService = new Intent(MainActivity.ActivityCurrent, typeof(DeviceUsageMonitorService));
#pragma warning restore CS8604
		startService.SetAction("START_SERVICE");
		MainActivity.ActivityCurrent.StartService(startService);
	}

	public void Stop()
	{
		Intent stopIntent = new Intent(MainActivity.ActivityCurrent, this.Class);
		stopIntent.SetAction("STOP_SERVICE");
		MainActivity.ActivityCurrent?.StartService(stopIntent);
	}

	private void OnBatteryInfoChanged(object? sender, BatteryInfoChangedEventArgs e)
	{
		CheckBatteryStatus();
	}

	private void CheckBatteryStatus()
	{
		var batteryInfo = Battery.Default.ChargeLevel;
		var chargeLevel = batteryInfo * 100;
		bool isCharging = Battery.Default.State == BatteryState.Charging;

		// Check if the battery percentage when charging is outside the range of 20-80%
		if (isCharging && (chargeLevel < 20 || chargeLevel > 80))
		{
			// Temporary Notification to test trigger
			_notificationManagerService.SendNotification("Unsustainable Charging", "Charging range outside sustainable range of 20-80%", false);
		}
	}

}
