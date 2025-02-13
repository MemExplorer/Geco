namespace Geco.PermissionHelpers;

public class ActivityResultEvent(int requestCode) : EventArgs
{
	public int RequestCode { get; } = requestCode;
}
