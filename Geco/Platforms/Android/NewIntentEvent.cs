using Android.Content;

namespace Geco;

public class NewIntentEvent(Intent? intentArg) : EventArgs
{
	public Intent? Intent { get; private set; } = intentArg;
}
