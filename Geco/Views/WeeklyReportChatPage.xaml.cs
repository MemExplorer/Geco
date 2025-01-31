using Geco.ViewModels;

namespace Geco.Views;

public partial class WeeklyReportChatPage : ContentPage
{
	WeeklyReportChatViewModel CurrentViewModel { get; }

	public WeeklyReportChatPage(WeeklyReportChatViewModel vm)
	{
		BindingContext = vm;
		CurrentViewModel = vm;
		InitializeComponent();
	}

	private async void WebView_Navigated(object sender, WebNavigatedEventArgs e)
	{
		try
		{
			if (sender is not WebView w)
				return;

			string backgroundColor = GecoSettings.DarkMode ? "#1C1C1C" : "#FFFFFF";
			string textColor = GecoSettings.DarkMode ? "#ffffff" : "#000000";
			await w.EvaluateJavaScriptAsync($$"""
			                                  (function() {
			                                  	function modifyStyles(backgroundColor, textColor) {
			                                  		document.body.style.backgroundColor = backgroundColor; 
			                                  		document.body.style.color = textColor; 
			                                  	}
			                                  
			                                  	modifyStyles('{{backgroundColor}}', '{{textColor}}');
			                                  })();
			                                  """);

			// load md to html converter script
			await using var stream = await FileSystem.OpenAppPackageFileAsync("showdown.min.js");
			using var reader = new StreamReader(stream);
			string showdownJs = await reader.ReadToEndAsync();
			await w.EvaluateJavaScriptAsync($$"""
			                                  {{showdownJs}}
			                                  var converter = new showdown.Converter();
			                                  converter.setOption('tables', true);
			                                  converter.setOption('simpleLineBreaks', true);
			                                  converter.setOption('requireSpaceBeforeHeadingText', true);
			                                  converter.setOption('simplifiedAutoLink', true);
			                                  const contentElement = document.getElementById('gecocontent');
			                                  const wrComputeContent = document.getElementById('wrComputeContent');
			                                  const wrBreakdown = document.getElementById('wrBreakdown');
			                                  const wrOverview = document.getElementById('wrOverview');
			                                  if (wrComputeContent && wrBreakdown && wrOverview)
			                                  {
			                                      wrComputeContent.innerHTML = converter.makeHtml(wrComputeContent.innerHTML);
			                                      wrBreakdown.innerHTML = converter.makeHtml(wrBreakdown.innerHTML);
			                                      wrOverview.innerHTML = converter.makeHtml(wrOverview.innerHTML);
			                                  }
			                                  else
			                                      contentElement.innerHTML = converter.makeHtml(contentElement.innerHTML);
			                                  """);
		}
		catch (Exception ex)
		{
			GlobalContext.Logger.Error<WeeklyReportChatPage>(ex);
		}
	}

	void WebView_OnNavigating(object? sender, WebNavigatingEventArgs e)
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
			GlobalContext.Logger.Error<WeeklyReportChatPage>(exception);
		}
	}

	void ChatEntry_OnTextChanged(object? sender, TextChangedEventArgs e) =>
		CurrentViewModel.ChatTextChanged(e.NewTextValue);
}
