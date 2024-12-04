using Geco.ViewModels;
using Geco.Models;
using Geco.Models.Monitor;

namespace Geco.Views;

public partial class SettingsPage : ContentPage
{
	public readonly IMonitorManagerService _monitorService;

	public SettingsPage(IMonitorManagerService monitorManagerService)
	{
		InitializeComponent();
		Appearing += SettingsPage_Appearing;
		_monitorService = monitorManagerService;
	}

	void SettingsPage_Appearing(object? sender, EventArgs e)
	{
		var bindingCtx = (SettingsViewModel)BindingContext;
		bindingCtx.LoadSettings(DarkModeSwt, MonitorSwt, NotificationSwt);
	}

	void NotificationToggle_Toggled(object sender, ToggledEventArgs e)
	{
		var bindingCtx = (SettingsViewModel)BindingContext;
		bindingCtx.ToggleNotifications((Switch)sender, e);
	}

	void MonitorSwt_Toggled(object sender, ToggledEventArgs e)
	{
		var bindingCtx = (SettingsViewModel)BindingContext;
		bindingCtx.ToggleMonitor((Switch)sender, e, (IMonitorManagerService) _monitorService);
	}
}
