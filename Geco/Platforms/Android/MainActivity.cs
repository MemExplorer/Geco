using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

namespace Geco;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop,
	ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
						   ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
	public static event EventHandler<NewIntentEvent>? OnNewIntentEvent;

	public static MainActivity? ActivityCurrent { get; set; }

	public MainActivity()
	{
		ActivityCurrent = this;
	}

	protected override void OnNewIntent(Intent? intent)
	{
		OnNewIntentEvent?.Invoke(this, new NewIntentEvent(intent));
		base.OnNewIntent(intent);
	}
}
