using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Geco.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
	[RelayCommand]
	void ToggleDarkMode(ToggledEventArgs e)
	{
		bool isDark = e.Value;
		Application.Current!.UserAppTheme = isDark ? AppTheme.Dark : AppTheme.Light;
	}
}
