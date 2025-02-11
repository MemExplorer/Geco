using CommunityToolkit.Maui.Alerts;
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

		vList.Adapter.ItemRangeInserted += Adapter_ItemRangeInserted;
	}

	private void Adapter_ItemRangeInserted(object? sender, (int startingIndex, int totalCount) e)
	{
		// Scroll to bottom effect after sending a message
		var scrollComponentItems = vList.LayoutManager.ReadOnlyLaidOutItems.Last();
		vList.ScrollToAsync(0, scrollComponentItems.LeftTop.Y, true);
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
		}
		catch (Exception exception)
		{
			GlobalContext.Logger.Error<WeeklyReportChatPage>(exception);
		}
	}

	void ChatEntry_OnTextChanged(object? sender, TextChangedEventArgs e) =>
		CurrentViewModel.ChatTextChanged(e.NewTextValue);

	async void TapGestureRecognizer_OnTapped(object? sender, TappedEventArgs e)
	{
		try
		{
			if (e.Parameter is not WebView wv)
				return;

			string copiedTxt = await wv.EvaluateJavaScriptAsync("document.body.innerText");
			await Clipboard.SetTextAsync(copiedTxt);
			await Toast.Make("Copied text to clipboard.").Show();
		}
		catch (Exception ex)
		{
			GlobalContext.Logger.Error<WeeklyReportChatPage>(ex);
		}
	}
}
