
using Android.App;
using Android.Content;
using Android.Widget;
using Geco.Core.Models.ActionObserver;

namespace Geco;
[BroadcastReceiver(Enabled = true, Exported = true, DirectBootAware = true)]
[IntentFilter(new[] { Intent.ActionBootCompleted })]
public class BootReceiver : BroadcastReceiver
{
	public override void OnReceive(Context? context, Intent? intent)
	{
		if (intent?.Action != Intent.ActionBootCompleted)
			return;

		try
		{
			var monitorEnabled = Preferences.Get(nameof(GecoSettings.Monitor), false);
			var serviceProvider = App.Current?.Handler.MauiContext?.Services!;
			var monitorService = serviceProvider.GetService<IPlatformActionObserver>();
			if (monitorEnabled)
				monitorService?.Start();
		}
		catch (Exception ex)
		{
			// CommunityToolkit Alert doesn't work properly here
			Toast.MakeText(context, ex.Message, ToastLength.Short).Show();
		}
	}
}

