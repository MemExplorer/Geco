using Android.Net;
using Geco.Core.Database;
using Geco.Models.DeviceState;

namespace Geco;

internal class NetworkStateObserver : IDeviceStateObserver
{
	private readonly NetworkCallbackHandler _networkCbHandler;
	private readonly ConnectivityManager _connectivityMgr;
	public event EventHandler<TriggerEventArgs>? OnStateChanged;

	public NetworkStateObserver()
	{
		var connectivitySvc = (ConnectivityManager?)Platform.AppContext.GetSystemService("connectivity");

		_connectivityMgr = connectivitySvc ?? throw new Exception("ConnectivityManager is null! That was unexpected.");
		_networkCbHandler = new NetworkCallbackHandler(_connectivityMgr);
	}

	public void StartEventListener()
	{
		_networkCbHandler.OnNetworkChange += OnNetworkChange;
		_connectivityMgr.RegisterNetworkCallback(new NetworkRequest.Builder().Build()!, _networkCbHandler);
	}

	private void OnNetworkChange(object? sender, NetworkChangedEventArgs e)
	{
		DeviceInteractionTrigger triggerType;
		if (OperatingSystem.IsAndroidVersionAtLeast(28))
		{
			var networkCapabilities = _connectivityMgr.GetNetworkCapabilities(e.Network);
			triggerType = networkCapabilities != null && networkCapabilities.HasTransport(TransportType.Cellular)
				? DeviceInteractionTrigger.NetworkUsageUnsustainable
				: DeviceInteractionTrigger.NetworkUsageSustainable;
		}
		else
		{
			triggerType = _connectivityMgr.GetNetworkInfo(e.Network)?.Type == ConnectivityType.Mobile
				? DeviceInteractionTrigger.NetworkUsageUnsustainable
				: DeviceInteractionTrigger.NetworkUsageSustainable;
		}

		OnStateChanged?.Invoke(sender, new TriggerEventArgs(triggerType, e));
	}

	public void StopEventListener()
	{
		_networkCbHandler.OnNetworkChange -= OnNetworkChange;
		try
		{
			_connectivityMgr.UnregisterNetworkCallback(_networkCbHandler);
		}
		catch
		{
			// handle **Java.Lang.IllegalArgumentException:** 'NetworkCallback was not registered'
		}
	}
}

public class NetworkChangedEventArgs(ConnectivityManager connectivity, Network network) : EventArgs
{
	public Network Network { get; } = network;
	public ConnectivityManager ConnectivityMgr { get; } = connectivity;
}

class NetworkCallbackHandler(ConnectivityManager connectivity) : ConnectivityManager.NetworkCallback
{
	private readonly ConnectivityManager _connectivityMgr = connectivity;
	public event EventHandler<NetworkChangedEventArgs>? OnNetworkChange;

	public override void OnAvailable(Network network)
	{
		OnNetworkChange?.Invoke(null, new NetworkChangedEventArgs(_connectivityMgr, network));
		base.OnAvailable(network);
	}

	public override async void OnLost(Network network)
	{
		try
		{
			// wait 5 seconds to ensure _connectivityMgr.ActiveNetwork is not null
			// and also wait for the device to connect to a new network
			await Task.Delay(5000);

			var activeNetwork = _connectivityMgr.ActiveNetwork;
			if (activeNetwork == null)
				return;

			OnNetworkChange?.Invoke(null, new NetworkChangedEventArgs(_connectivityMgr, activeNetwork));
			base.OnLost(network);
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
		}
	}
}
