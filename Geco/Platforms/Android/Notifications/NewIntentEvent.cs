using Android.Content;

namespace Geco.Notifications;

public class NewIntentEvent(Intent? intentArg) : EventArgs
{
	public Intent? Intent { get; private set; } = intentArg;
}
