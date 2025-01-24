using Geco.Core.Brave;

namespace Geco.Views;

public partial class SearchResultPage : ContentPage
{
	public SearchResultPage() => InitializeComponent();

	async void TapGestureRecognizer_OnTapped(object? sender, TappedEventArgs e)
	{
		try
		{
			if (e.Parameter is not WebResultEntry wre)
				return;

			var options = new BrowserLaunchOptions
			{
				LaunchMode = BrowserLaunchMode.SystemPreferred,
				TitleMode = BrowserTitleMode.Show,
				PreferredToolbarColor = Color.Parse(GecoSettings.DarkMode ? "#324b4a" : "#e1edec"),
				PreferredControlColor = Color.Parse(GecoSettings.DarkMode ? "#e1edec" : "#324b4a")
			};

			await Browser.Default.OpenAsync(wre.Url, options);
		}
		catch (Exception ex)
		{
			GlobalContext.Logger.Error<SearchResultPage>(ex);
		}
	}
}
