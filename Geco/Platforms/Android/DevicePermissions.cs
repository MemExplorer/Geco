using Android;

namespace Geco;

internal class DevicePermissions : Permissions.BasePlatformPermission
{
	public override (string androidPermission, bool isRuntime)[] RequiredPermissions
	{
		get
		{
			var result = new List<(string androidPermission, bool isRuntime)>();
			if (OperatingSystem.IsAndroidVersionAtLeast(33))
				result.Add((Manifest.Permission.PostNotifications, true));

			if (OperatingSystem.IsAndroidVersionAtLeast(23))
				result.Add((Manifest.Permission.BatteryStats, true));

			if(OperatingSystem.IsAndroidVersionAtLeast(28))
				// Permission for Foreground Service
				result.Add((Manifest.Permission.ForegroundService, true));

			return result.ToArray();
		}
	}

	


}
