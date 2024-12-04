namespace Geco.Models.Notifications;

public interface INotificationManagerService
{
	public event EventHandler<GecoTriggerEventMessage>? OnNotificationClick;
	void SendNotification(string title, string message, bool isForeground);
}
