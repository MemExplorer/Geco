
using Android.App;
using Android.Content;
using Geco.Models.Notifications;

namespace Geco;

[BroadcastReceiver(Exported = true, Enabled = true)]
[IntentFilter(new[] { "com.ssbois.geco.ScheduledTaskReceiver" })]
internal class ScheduledTaskReceiver : BroadcastReceiver
{
	public override void OnReceive(Context? context, Intent? intent)
	{
		if (intent?.Action != "schedtaskcmd")
			return;

		var SvcProvider = App.Current?.Handler.MauiContext?.Services!;
		var NotificationSvc = SvcProvider.GetService<INotificationManagerService>()!;

		// run code to record device usage and mobile data
		NotificationSvc.SendNotification("Task test", "Received scheduled task");
	}
}
