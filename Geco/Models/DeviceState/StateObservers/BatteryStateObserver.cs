
using Geco.Core.Database;

namespace Geco.Models.DeviceState.StateObservers;
internal class BatteryStateObserver : IDeviceStateObserver
{
	public event EventHandler<TriggerEventArgs>? OnStateChanged;

	public void StartEventListener() => Battery.Default.BatteryInfoChanged += OnBatteryInfoChanged;

	public void StopEventListener() => Battery.Default.BatteryInfoChanged -= OnBatteryInfoChanged;

	private void OnBatteryInfoChanged(object? sender, BatteryInfoChangedEventArgs e)
	{
		var batteryInfo = Battery.Default.ChargeLevel;
		var chargeLevel = batteryInfo * 100;
		bool isCharging = Battery.Default.State == BatteryState.Charging;
		DeviceInteractionTrigger triggerType;

		// Check if the battery percentage when charging is outside the range of 20-80%
		if (isCharging && Battery.Default.PowerSource != BatteryPowerSource.Battery && (chargeLevel < 20 || chargeLevel > 80))
			triggerType = DeviceInteractionTrigger.ChargingUnsustainable;
		else
			triggerType = DeviceInteractionTrigger.ChargingSustainable;

		OnStateChanged?.Invoke(sender, new TriggerEventArgs(triggerType, e));
	}
}
