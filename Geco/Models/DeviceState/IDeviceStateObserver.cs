using Geco.Core.Database;

namespace Geco.Models.Monitor;
internal class TriggerEventArgs(DeviceInteractionTrigger triggerType, object eventData) : EventArgs
{
	internal DeviceInteractionTrigger TriggerType { get; } = triggerType;
	internal object EventData { get; } = eventData;
}

internal interface IDeviceStateObserver
{
	event EventHandler<TriggerEventArgs>? OnStateChanged;
	void StartEventListener();
	void StopEventListener();
}
