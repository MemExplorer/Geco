using Geco.Core.Brave;
using Geco.ViewModels;

namespace Geco.Views;

public partial class SearchResultPage : ContentPage
{
	readonly SearchResultViewModel CurrentViewModel;

	public SearchResultPage(SearchResultViewModel vm)
	{
		BindingContext = vm;
		CurrentViewModel = vm;
		InitializeComponent();
	}

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

	void AIOverviewWebview_OnNavigating(object? sender, WebNavigatingEventArgs e)
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
			GlobalContext.Logger.Error<SearchResultPage>(exception);
		}
	}

	void SearchEntry_OnTextChanged(object? sender, TextChangedEventArgs e) =>
		CurrentViewModel.SearchTextChanged(e.NewTextValue);
}
