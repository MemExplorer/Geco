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
	private LocationManager _locationManager { get; }
	private LocationListenerCallback _locationListenerCb { get; }
	public LocationStateObserver()
	{
		var locationMgr = (LocationManager?)Platform.AppContext.GetSystemService(Context.LocationService);
		_locationManager = locationMgr ?? throw new Exception("LocationManager is null! That was unexpected.");
		_locationListenerCb = new LocationListenerCallback();
	}

	public void StartEventListener()
	{
		_locationListenerCb.OnToggle += OnLocationSvcToggle;
		_locationManager.RequestLocationUpdates(LocationManager.GpsProvider, long.MaxValue, float.MaxValue, _locationListenerCb);
	}

	private void OnLocationSvcToggle(object? sender, LocationStatusEventArgs e)
	{
		var isSustainable = e.IsToggled ? DeviceInteractionTrigger.LocationUsageUnsustainable : DeviceInteractionTrigger.LocationUsageSustainable;
		OnStateChanged?.Invoke(null, new TriggerEventArgs(isSustainable, e));
	}

	public void StopEventListener()
	{
		_locationListenerCb.OnToggle -= OnLocationSvcToggle;
		_locationManager.RemoveUpdates(_locationListenerCb);
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

	public void OnProviderDisabled(string provider)
	{
		OnToggle?.Invoke(null, new LocationStatusEventArgs(false));
	}
	public void OnProviderEnabled(string provider)
	{
		OnToggle?.Invoke(null, new LocationStatusEventArgs(true));
	}
	public void OnStatusChanged(string? provider, [GeneratedEnum] Availability status, Bundle? extras)
	{
		// not needed
	}
}
