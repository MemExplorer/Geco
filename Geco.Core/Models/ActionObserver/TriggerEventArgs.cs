namespace Geco.Core.Models.ActionObserver;

public class TriggerEventArgs(DeviceInteractionTrigger triggerType, object eventData) : EventArgs
{
	public DeviceInteractionTrigger TriggerType { get; } = triggerType;
	internal object EventData { get; } = eventData;
}
