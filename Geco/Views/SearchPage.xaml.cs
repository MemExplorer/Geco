using InputKit.Shared.Controls;

namespace Geco.Views;

public partial class SearchPage : ContentPage
{
	public SearchPage()
	{
		InitializeComponent();

		Appearing += OnAppearing;
	}

	void OnAppearing(object? sender, EventArgs e)
	{
		// temporary solution for theme bug
		bool isDarkTheme = Application.Current!.RequestedTheme == AppTheme.Dark;
		foreach (var currentBtn in BtnSelection.Children)
		{
			if (currentBtn is not SelectionView.SelectableButton sb)
				continue;

			sb.UnselectedColor = isDarkTheme ? Color.FromArgb("#FF262626") : Colors.LightGray;
			sb.SelectedColor = isDarkTheme ? Color.FromArgb("#FF141414") : Color.FromArgb("#FFb5b5b5");
		}
	}
}
