namespace Geco.Core.Models.ActionObserver;

public interface IDeviceStateObserver
{
	event EventHandler<TriggerEventArgs>? OnStateChanged;
	void StartEventListener();
	void StopEventListener();
}
