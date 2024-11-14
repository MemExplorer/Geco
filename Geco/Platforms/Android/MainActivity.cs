using Android.App;
using Android.Content;
using Android.Content.PM;

namespace Geco
{
	[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop,
		ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
		                       ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
	public class MainActivity : MauiAppCompatActivity
	{
		public static event EventHandler<NewIntentEvent>? OnNewIntentEvent;

		protected override void OnNewIntent(Intent? intent)
		{
			OnNewIntentEvent?.Invoke(this, new(intent));
			base.OnNewIntent(intent);
		}
	}
}
