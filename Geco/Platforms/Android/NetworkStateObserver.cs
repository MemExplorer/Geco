
using Android.Net;
using Geco.Core.Database;
using Geco.Models.Monitor;

namespace Geco.Platforms.Android;
internal class NetworkStateObserver : IDeviceStateObserver
{
	private NetworkCallbackHandler _networkCbHandler;
	private ConnectivityManager _connectivityMgr;
	public event EventHandler<TriggerEventArgs>? OnStateChanged;

	public NetworkStateObserver()
	{
		var connectivitySvc = (ConnectivityManager?)Platform.AppContext.GetSystemService("connectivity");
		if (connectivitySvc == null)
			throw new Exception("ConnectivityManager is null! That was unexpected.");

		_connectivityMgr = connectivitySvc;
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
		if (OperatingSystem.IsAndroidVersionAtLeast(23))
		{
			var networkCapabilities = _connectivityMgr.GetNetworkCapabilities(e.Network);
			triggerType = networkCapabilities != null && networkCapabilities.HasTransport(TransportType.Cellular) ?
				DeviceInteractionTrigger.NetworkUsageUnsustainable : DeviceInteractionTrigger.NetworkUsageSustainable;
		}
		else
		{
			triggerType = _connectivityMgr.GetNetworkInfo(e.Network)?.Type == ConnectivityType.Mobile ?
				DeviceInteractionTrigger.NetworkUsageUnsustainable : DeviceInteractionTrigger.NetworkUsageSustainable;
		}

		OnStateChanged?.Invoke(sender, new TriggerEventArgs(triggerType, e));
	}

	public void StopEventListener()
	{
		_networkCbHandler.OnNetworkChange -= OnNetworkChange;
		_connectivityMgr.UnregisterNetworkCallback(_networkCbHandler);
	}
}

public class NetworkChangedEventArgs(ConnectivityManager connectivity, Network network, bool lost) : EventArgs
{
	public Network Network { get; } = network;
	public ConnectivityManager ConnectivityMgr { get; } = connectivity;
	public bool Lost { get; } = lost;
}

class NetworkCallbackHandler(ConnectivityManager connectivity) : ConnectivityManager.NetworkCallback
{
	private ConnectivityManager _connectivityMgr = connectivity;
	public event EventHandler<NetworkChangedEventArgs>? OnNetworkChange;
	public override void OnAvailable(Network network)
	{
		OnNetworkChange?.Invoke(null, new NetworkChangedEventArgs(_connectivityMgr, network, false));
		base.OnAvailable(network);
	}

	public override void OnLost(Network network)
	{
		OnNetworkChange?.Invoke(null, new NetworkChangedEventArgs(_connectivityMgr, network, true));
		base.OnLost(network);
	}
}
