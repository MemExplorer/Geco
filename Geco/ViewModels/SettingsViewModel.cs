using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Geco.ViewModels;
public partial class SettingsViewModel : ObservableObject
{
	[RelayCommand]
	public void ToggleDarkMode(ToggledEventArgs e)
	{
		bool IsDark = e.Value;
		App.Current!.UserAppTheme = IsDark ? AppTheme.Dark : AppTheme.Light;
	}
}
