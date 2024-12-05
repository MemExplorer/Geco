namespace Geco.Models.Notifications;

public class GecoNotificationMessageEvent(string triggerEvtMessage) : EventArgs
{
	public string Message { get; private set; } = triggerEvtMessage;
}
