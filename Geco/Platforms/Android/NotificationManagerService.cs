using _Microsoft.Android.Resource.Designer;
using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.OS;
using AndroidX.Core.App;
using Geco.Models.Notifications;

namespace Geco;

public class NotificationManagerService : INotificationManagerService, IDisposable
{
	const string ChannelId = "gecoChannelId";
	const string ChannelName = "Geco Channel Name";
	const string ChannelDescription = "The default channel for notifications.";
	const string IntentActionName = "GecoNotif";

	bool _channelInitialized = false;
	int _messageId = 0;
	int _pendingIntentId = 0;
	readonly NotificationManagerCompat _compatManager;

	public event EventHandler<GecoTriggerEventMessage>? OnNotificationClick;

	public NotificationManagerService()
	{
		CreateNotificationChannel();
		_compatManager = NotificationManagerCompat.From(Platform.AppContext);
		MainActivity.OnNewIntentEvent += OnNewIntentEvent;
	}

	private void OnNewIntentEvent(object? sender, NewIntentEvent e)
	{
		if (e.Intent == null || e.Intent.Action != IntentActionName)
			return;

		var msgContent = e.Intent.GetStringExtra("message");
		OnNotificationClick?.Invoke(this, new(msgContent!));
	}

	public void SendNotification(string title, string message)
	{
		if (!_channelInitialized)
		{
			CreateNotificationChannel();
		}

		Show(title, message);
	}

	public void Show(string title, string message)
	{
		var intent = new Intent(Platform.AppContext, typeof(MainActivity));
		intent.SetAction(IntentActionName);
		intent.PutExtra("message", message);

		var pendingIntentFlags = (Build.VERSION.SdkInt >= BuildVersionCodes.S)
#pragma warning disable CA1416
			? PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable
			: PendingIntentFlags.UpdateCurrent;
#pragma warning restore CA1416

		var pendingIntent =
			PendingIntent.GetActivity(Platform.AppContext, _pendingIntentId++, intent, pendingIntentFlags);
		var builder = new NotificationCompat.Builder(Platform.AppContext, ChannelId)
			.SetContentIntent(pendingIntent)
			.SetContentTitle(title)
			.SetContentText(message)
			.SetSmallIcon(ResourceConstant.Drawable.geco_logo)
			.SetLargeIcon(BitmapFactory.DecodeResource(Platform.AppContext.Resources,
				ResourceConstant.Drawable.geco_logo))
			.SetStyle(new NotificationCompat.BigTextStyle().BigText(message))
			.SetAutoCancel(true);

		var notification = builder.Build();
		_compatManager.Notify(_messageId++, notification);
	}

	void CreateNotificationChannel()
	{
		// Create the notification channel, but only on API 26+.
		if (Build.VERSION.SdkInt < BuildVersionCodes.O)
			return;

		var channelNameJava = new Java.Lang.String(ChannelName);
#pragma warning disable CA1416
		var channel = new NotificationChannel(ChannelId, channelNameJava, NotificationImportance.Default)
		{
			Description = ChannelDescription
		};
#pragma warning restore CA1416
		// Register the channel
		var manager = (NotificationManager)Platform.AppContext.GetSystemService(Context.NotificationService)!;
#pragma warning disable CA1416
		manager.CreateNotificationChannel(channel);
#pragma warning restore CA1416
		_channelInitialized = true;
	}

	public void Dispose() => MainActivity.OnNewIntentEvent -= OnNewIntentEvent;
}
