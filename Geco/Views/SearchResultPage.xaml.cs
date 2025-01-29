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

			await Utils.OpenBrowserView(wre.Url.ToString());
		}
		catch (Exception ex)
		{
			GlobalContext.Logger.Error<SearchResultPage>(ex);
		}
	}

	async void WebView_OnNavigated(object? sender, WebNavigatedEventArgs e)
	{
		try
		{
			if (sender is not WebView w) 
				return;
		
			string backgroundColor = GecoSettings.DarkMode ? "#1C1C1C" : "#FFFFFF";
			string textColor = GecoSettings.DarkMode ? "#ffffff" : "#000000";

			// load md to html converter script
			await using var stream = await FileSystem.OpenAppPackageFileAsync("showdown.min.js");
			using var reader = new StreamReader(stream);
			var showdownJs = await reader.ReadToEndAsync();
		
			await w.EvaluateJavaScriptAsync($$"""
			                                  {{showdownJs}}
			                                  var converter = new showdown.Converter();
			                                  converter.setOption('tables', true);
			                                  converter.setOption('simpleLineBreaks', true);
			                                  converter.setOption('headerLevelStart', 2);
			                                  converter.setOption('simplifiedAutoLink', true);
			                                  converter.setOption('requireSpaceBeforeHeadingText', true);
			                                  const contentElement = document.getElementById('gecocontent');
			                                  contentElement.innerHTML = converter.makeHtml(contentElement.innerHTML);
			                                  (function() {
			                                  	function modifyStyles(backgroundColor, textColor) {
			                                  		document.body.style.backgroundColor = backgroundColor; 
			                                  		document.body.style.color = textColor; 
			                                  	}
			                                  
			                                  	modifyStyles('{{backgroundColor}}', '{{textColor}}');
			                                  })();
			                                  """);
		}
		catch (Exception ex)
		{
			GlobalContext.Logger.Error<SearchResultPage>(ex);
		}
	}

	void AIOverviewWebview_OnNavigating(object? sender, WebNavigatingEventArgs e)
	{
		try
		{
			if (e.Url.StartsWith("https://") || e.Url.StartsWith("http://"))
			{
				e.Cancel = true;
				_ = Utils.OpenBrowserView(e.Url);
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
