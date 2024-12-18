namespace Geco.Core.Models.Notification;

public class GecoNotificationMessageEvent(string triggerEvtMessage) : EventArgs
{
	public string Message { get; private set; } = triggerEvtMessage;
}
