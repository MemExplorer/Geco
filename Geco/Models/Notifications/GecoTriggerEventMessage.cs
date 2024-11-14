namespace Geco.Models.Notifications;

public class GecoTriggerEventMessage(string triggerEvtMessage) : EventArgs
{
	public string Message { get; private set; } = triggerEvtMessage;
}
