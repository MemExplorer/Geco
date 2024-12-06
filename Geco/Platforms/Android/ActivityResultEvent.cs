
namespace Geco;
public class ActivityResultEvent(int requestCode) : EventArgs
{
	public int RequestCode { get; } = requestCode;
}
