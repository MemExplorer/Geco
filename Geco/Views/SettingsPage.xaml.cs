using Geco.ViewModels;

namespace Geco.Views;

public partial class SettingsPage : ContentPage
{
	public SettingsPage()
	{
		InitializeComponent();
		Appearing += SettingsPage_Appearing;
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
}
