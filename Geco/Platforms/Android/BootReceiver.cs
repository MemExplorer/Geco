
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
			var monitorService = GlobalContext.Services.GetRequiredService<IPlatformActionObserver>();
			if (GecoSettings.Monitor)
				monitorService.Start();
		}
		catch (Exception ex)
		{
			// CommunityToolkit Alert doesn't work properly here
			Toast.MakeText(context, ex.Message, ToastLength.Short).Show();
		}
	}
}

