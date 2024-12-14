namespace Geco.Models.Notifications;

public interface INotificationManagerService
{
	public event EventHandler<GecoNotificationMessageEvent>? OnNotificationClick;
	void SendNotification(string title, string message);
}
