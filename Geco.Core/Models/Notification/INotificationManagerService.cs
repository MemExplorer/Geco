namespace Geco.Core.Models.Notification;

public interface INotificationManagerService
{
#if ANDROID
	void RunNotification(Android.App.Notification notification);
	Android.App.Notification? SendPersistentNotification(string title, string description);
#endif
	void SendInteractiveNotification(string title, string description, string message);
	void SendInteractiveWeeklyReportNotification(string id, string title, string description, string message);
}
