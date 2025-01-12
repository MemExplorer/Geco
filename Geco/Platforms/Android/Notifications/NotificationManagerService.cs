using _Microsoft.Android.Resource.Designer;
using Android.App;
using Android.Content;
using Android.Graphics;
using AndroidX.Core.App;
using Geco.Core.Models.Notification;
using String = Java.Lang.String;

namespace Geco.Notifications;

public class NotificationManagerService : INotificationManagerService
{
	const string ChannelId = "gecoChannelId";
	const string ChannelName = "Geco Channel Name";
	const string ChannelDescription = "The default channel for notifications.";
	const string IntentActionName = "GecoNotif";

	bool _channelInitialized;
	int _messageId;
	int _pendingIntentId;
	readonly NotificationManagerCompat _compatManager;

	public NotificationManagerService()
	{
		CreateNotificationChannel();
		_compatManager = NotificationManagerCompat.From(Platform.AppContext);
		MainActivity.OnNewIntentEvent += async (s, e) =>
			await OnNewIntentEvent(s, e);
	}

	private async Task OnNewIntentEvent(object? sender, NewIntentEvent e)
	{
		if (e.Intent is not { Action: IntentActionName })
			return;

		Platform.CurrentActivity!.Intent = e.Intent;
		await Shell.Current.Navigation.PopToRootAsync();
		await Shell.Current.GoToAsync("///IMPL_ChatPage");
	}

	public void RunNotification(Notification notification)
	{
		if (!_channelInitialized)
			CreateNotificationChannel();

		_compatManager.Notify(_messageId, notification);
	}

	public Notification SendPersistentNotification(string title, string description)
	{
		var notificationInst = InternalSendNotification(title, description, description, false, false);
		notificationInst.Flags = NotificationFlags.OngoingEvent;
		return notificationInst;
	}

	public void SendInteractiveNotification(string title, string description) =>
		InternalSendNotification(title, description, description, true);

	public void SendInteractiveNotification(string title, string description, string message) =>
		InternalSendNotification(title, description, message, true);

	private Notification InternalSendNotification(string title, string description, string message, bool interactive,
		bool notify = true)
	{
		if (!_channelInitialized)
		{
			CreateNotificationChannel();
		}

		var notificationInstance = Show(title, description, message, interactive);

		if (notify)
			_compatManager.Notify(_messageId++, notificationInstance);

		return notificationInstance;
	}

	public Notification Show(string title, string description, string message, bool interactive)
	{
		var builder = new NotificationCompat.Builder(Platform.AppContext, ChannelId)
			.SetContentTitle(title)
			.SetContentText(description)
			.SetSmallIcon(ResourceConstant.Drawable.geco_logo)
			.SetLargeIcon(BitmapFactory.DecodeResource(Platform.AppContext.Resources,
				ResourceConstant.Drawable.geco_logo))
			.SetStyle(new NotificationCompat.BigTextStyle().BigText(description))
			.SetAutoCancel(true);

		if (interactive)
		{
			var intent = new Intent(Platform.AppContext, typeof(MainActivity));
			intent.SetAction(IntentActionName);
			intent.PutExtra("message", message);
			intent.PutExtra("title", title);

			var pendingIntentFlags = OperatingSystem.IsAndroidVersionAtLeast(31)
				? PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable
				: PendingIntentFlags.UpdateCurrent;

			var pendingIntent =
				PendingIntent.GetActivity(Platform.AppContext, _pendingIntentId++, intent, pendingIntentFlags);
			builder = builder.SetContentIntent(pendingIntent);
		}

		return builder.Build();
	}

	void CreateNotificationChannel()
	{
		// Create the notification channel, but only on API 26+.
		if (!OperatingSystem.IsAndroidVersionAtLeast(26))
			return;

		var channelNameJava = new String(ChannelName);
		var channel = new NotificationChannel(ChannelId, channelNameJava, NotificationImportance.Default);
		// Register the channel
		var manager = (NotificationManager)Platform.AppContext.GetSystemService(Context.NotificationService)!;
		manager.CreateNotificationChannel(channel);
		_channelInitialized = true;
	}
}
