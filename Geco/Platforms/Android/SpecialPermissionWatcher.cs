using Android.Content;
using AndroidNet = Android.Net;
namespace Geco;

internal class SpecialPermissionWatcher
{
	private string SettingId { get; }
	private string? PackageId { get; }
	private int RequestCode { get; }
	private Func<bool> CheckPermissionFunc { get; }
	private TaskCompletionSource<bool> TaskResult { get; }
	private bool GrantedPermission { get; set; }

	public SpecialPermissionWatcher(Func<bool> permChecker, string settingId, string? appPackageId = null)
	{
		SettingId = settingId;
		PackageId = appPackageId;
		RequestCode = Math.Abs(settingId.GetHashCode() * 8714);
		CheckPermissionFunc = permChecker;
		TaskResult = new TaskCompletionSource<bool>();
		GrantedPermission = false;
		MainActivity.OnActivityResultEvent += OnActivityResultEvent;
	}

	private void OnActivityResultEvent(object? sender, ActivityResultEvent e)
	{
		if (e.RequestCode != RequestCode) 
			return;
		
		GrantedPermission = CheckPermissionFunc();
		MainActivity.OnActivityResultEvent -= OnActivityResultEvent;
		TaskResult.SetResult(true);
	}

	public async Task<bool> RequestAsync()
	{
		var intent = new Intent(SettingId);
		if (PackageId != null)
			intent.SetData(AndroidNet.Uri.FromParts("package", PackageId, null));

		Platform.CurrentActivity!.StartActivityForResult(intent, RequestCode);
		await TaskResult.Task;
		return GrantedPermission;
	}
}
