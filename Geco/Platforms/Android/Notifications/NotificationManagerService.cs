using _Microsoft.Android.Resource.Designer;
using Android.App;
using Android.Content;
using Android.Graphics;
using AndroidX.Core.App;
using Geco.Core.Models.Notification;
using Geco.Views;
using String = Java.Lang.String;

namespace Geco.Notifications;

public class NotificationManagerService : INotificationManagerService
{
	public const string WeeklyReportNotificationId = "GecoWeeklyReportNotif";
	const string ChannelId = "gecoChannelId";
	const string ChannelName = "Geco Channel Name";
	const string ChannelDescription = "The default channel for notifications.";
	const string DefaultNotificationId = "GecoNotif";

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
		bool isWeeklyReport = e.Intent?.Action == WeeklyReportNotificationId;
		if (!(e.Intent?.Action == DefaultNotificationId || isWeeklyReport))
			return;

		Platform.CurrentActivity!.Intent = e.Intent;

		if (isWeeklyReport && e.Intent != null && e.Intent.HasExtra("historyid"))
		{
			// ensure `e.Intent.GetStringExtra` is not null
			string historyId = e.Intent.GetStringExtra("historyid")!;
			await Shell.Current.GoToAsync(nameof(WeeklyReportChatPage),
				new Dictionary<string, object> { { "historyid", historyId } });
		}
		else if (Shell.Current.CurrentItem.CurrentItem.Route == "IMPL_ChatPage" &&
		         Shell.Current.CurrentPage is ChatPage chatPage)
			await chatPage.InitializeChat(); // If we are already in chat page, reload viewmodel
		else
		{
			await Shell.Current.Navigation.PopToRootAsync();
			await Shell.Current.GoToAsync("///IMPL_ChatPage");
		}
	}

	public void RunNotification(Notification notification)
	{
		if (!_channelInitialized)
			CreateNotificationChannel();

		_compatManager.Notify(_messageId, notification);
	}

	public Notification SendPersistentNotification(string title, string description)
	{
		var notificationInst =
			InternalSendNotification(title, description, description, false, DefaultNotificationId, false);
		notificationInst.Flags = NotificationFlags.OngoingEvent;
		return notificationInst;
	}

	public void SendInteractiveNotification(string title, string description, string message) =>
		InternalSendNotification(title, description, message, true, DefaultNotificationId);

	public void SendInteractiveWeeklyReportNotification(string id, string title, string description, string message) =>
		InternalSendNotification(title, description, message, true, WeeklyReportNotificationId,
			args: new Dictionary<string, string> { { "historyid", id } });

	private Notification InternalSendNotification(string title, string description, string message, bool interactive,
		string notificationId,
		bool notify = true, Dictionary<string, string>? args = null)
	{
		if (!_channelInitialized)
		{
			CreateNotificationChannel();
		}

		var notificationInstance = Show(title, description, message, notificationId, interactive, args);

		if (notify)
			_compatManager.Notify(_messageId++, notificationInstance);

		return notificationInstance;
	}

	public Notification Show(string title, string description, string message, string notificationId, bool interactive,
		Dictionary<string, string>? args)
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
			intent.SetAction(notificationId);
			intent.PutExtra("message", message);
			intent.PutExtra("title", title);

			if (args != null)
			{
				foreach (var a in args)
					intent.PutExtra(a.Key, a.Value);
			}

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
