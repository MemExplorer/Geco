
namespace Geco.Models.Notifications;

public interface INotificationManagerService
{
	#if ANDROID
	void RunNotification(Android.App.Notification notification);
	Android.App.Notification? SendPersistentNotification(string title, string description);
	#endif
	void SendInteractiveNotification(string title, string description);
	void SendInteractiveNotification(string title, string description, string message);
}
