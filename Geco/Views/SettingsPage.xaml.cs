using Geco.Core.Models.ActionObserver;
using Geco.ViewModels;

namespace Geco.Views;

public partial class SettingsPage : ContentPage
{
	readonly IPlatformActionObserver _monitorService;

	public SettingsPage(IPlatformActionObserver platformActionObserver)
	{
		InitializeComponent();
		Appearing += SettingsPage_Appearing;
		_monitorService = platformActionObserver;
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
		bindingCtx.ToggleMonitor((Switch)sender, e, _monitorService);
	}
}
