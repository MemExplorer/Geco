using Geco.Core.Models.ActionObserver;

namespace Geco.Triggers.ActionObservers;

public class BatteryStateObserver : IDeviceStateObserver
{
	public event EventHandler<TriggerEventArgs>? OnStateChanged;

	public void StartEventListener() => Battery.Default.BatteryInfoChanged += OnBatteryInfoChanged;

	public void StopEventListener() => Battery.Default.BatteryInfoChanged -= OnBatteryInfoChanged;

	private void OnBatteryInfoChanged(object? sender, BatteryInfoChangedEventArgs e)
	{
		double batteryInfo = Battery.Default.ChargeLevel;
		double chargeLevel = batteryInfo * 100;
		bool isCharging = Battery.Default.State == BatteryState.Charging;

		// Check if the battery percentage when charging is outside the range of 20-80%
		if (isCharging && Battery.Default.PowerSource != BatteryPowerSource.Battery)
		{
			var triggerType = chargeLevel is < 20 or > 80
				? DeviceInteractionTrigger.ChargingUnsustainable
				: DeviceInteractionTrigger.ChargingSustainable;
			OnStateChanged?.Invoke(sender, new TriggerEventArgs(triggerType, e));
		}
	}
}
