using Android.Content;
using Android.Locations;
using Android.OS;
using Android.Runtime;
using Geco.Core.Database;
using Geco.Models.DeviceState;

namespace Geco;
internal class LocationStateObserver : IDeviceStateObserver
{
	public event EventHandler<TriggerEventArgs>? OnStateChanged;
	private LocationManager LocationMgrInst { get; }
	private LocationListenerCallback LocationListenerCb { get; }
	public LocationStateObserver()
	{
		var locationMgr = (LocationManager?)Platform.AppContext.GetSystemService(Context.LocationService);
		LocationMgrInst = locationMgr ?? throw new Exception("LocationManager is null! That was unexpected.");
		LocationListenerCb = new LocationListenerCallback();
	}

	public void StartEventListener()
	{
		LocationListenerCb.OnToggle += OnLocationSvcToggle;
		LocationMgrInst.RequestLocationUpdates(LocationManager.GpsProvider, long.MaxValue, float.MaxValue, LocationListenerCb);
	}

	private void OnLocationSvcToggle(object? sender, LocationStatusEventArgs e)
	{
		var isSustainable = e.IsToggled ? DeviceInteractionTrigger.LocationUsageUnsustainable : DeviceInteractionTrigger.LocationUsageSustainable;
		OnStateChanged?.Invoke(null, new TriggerEventArgs(isSustainable, e));
	}

	public void StopEventListener()
	{
		LocationListenerCb.OnToggle -= OnLocationSvcToggle;
		LocationMgrInst.RemoveUpdates(LocationListenerCb);
	}
}

internal class LocationStatusEventArgs(bool isToggled) : EventArgs
{
	public bool IsToggled { get; } = isToggled;
}

class LocationListenerCallback : Java.Lang.Object, ILocationListener
{
	public event EventHandler<LocationStatusEventArgs>? OnToggle;
	public void OnLocationChanged(Android.Locations.Location location)
	{
		// not needed
	}

	public void OnProviderDisabled(string provider) => 
		OnToggle?.Invoke(null, new LocationStatusEventArgs(false));

	public void OnProviderEnabled(string provider) => 
		OnToggle?.Invoke(null, new LocationStatusEventArgs(true));

	public void OnStatusChanged(string? provider, [GeneratedEnum] Availability status, Bundle? extras)
	{
		// not needed
	}
}
