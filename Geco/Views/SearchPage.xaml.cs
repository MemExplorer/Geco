using Geco.ViewModels;

namespace Geco.Views;

public partial class SearchPage : ContentPage
{
	SearchViewModel CurrentViewModel { get; }

	public SearchPage(SearchViewModel vm)
	{
		BindingContext = vm;
		CurrentViewModel = vm;
		InitializeComponent();
	}

	void SearchEntry_OnTextChanged(object? sender, TextChangedEventArgs e) =>
		CurrentViewModel.SearchTextChanged(e.NewTextValue);
}
