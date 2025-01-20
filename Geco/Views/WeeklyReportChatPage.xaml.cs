namespace Geco.Views;

public partial class WeeklyReportChatPage : ContentPage
{
	public WeeklyReportChatPage() => InitializeComponent();

	private void WebView_Navigated(object sender, WebNavigatedEventArgs e)
	{
		if (sender is not WebView w)
			return;

		string backgroundColor = GecoSettings.DarkMode ? "#191919" : "#e3e3e3";
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
}
