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

			await OpenBrowserView(wre.Url.ToString());
		}
		catch (Exception ex)
		{
			GlobalContext.Logger.Error<SearchResultPage>(ex);
		}
	}

	async Task OpenBrowserView(string url)
	{
		var options = new BrowserLaunchOptions
		{
			LaunchMode = BrowserLaunchMode.SystemPreferred,
			TitleMode = BrowserTitleMode.Show,
			PreferredToolbarColor = Color.Parse(GecoSettings.DarkMode ? "#324b4a" : "#e1edec"),
			PreferredControlColor = Color.Parse(GecoSettings.DarkMode ? "#e1edec" : "#324b4a")
		};

		await Browser.Default.OpenAsync(url, options);
	}

	void WebView_OnNavigated(object? sender, WebNavigatedEventArgs e)
	{
		if (sender is not WebView w)
			return;

		string backgroundColor = GecoSettings.DarkMode ? "#1C1C1C" : "#FFFFFF";
		string textColor = GecoSettings.DarkMode ? "#ffffff" : "#000000";

		w.EvaluateJavaScriptAsync(@$"
				(function() {{
					function modifyStyles(backgroundColor, textColor) {{
						document.body.style.overflow = 'hidden'; 
						document.body.style.backgroundColor = backgroundColor; 
						document.body.style.color = textColor; 
					}}

					modifyStyles('{backgroundColor}', '{textColor}');
				}})();
			");
	}

	void AIOverviewWebview_OnNavigating(object? sender, WebNavigatingEventArgs e)
	{
		try
		{
			if (e.Url.StartsWith("https://") || e.Url.StartsWith("http://"))
			{
				e.Cancel = true;
				_ = OpenBrowserView(e.Url);
			}
			else if (!e.Url.StartsWith("data:text/html;base64,"))
				e.Cancel = true;
		}
		catch (Exception exception)
		{
			GlobalContext.Logger.Error<SearchResultPage>(exception);
		}
	}
}
