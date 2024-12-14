using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Geco.Views;

namespace Geco.ViewModels;
public partial class SearchViewModel : ObservableObject
{
	[ObservableProperty] string? searchQuery;
	[ObservableProperty] string? selectedCategory;

	[RelayCommand]
	async Task Search(Entry searchEntry)
	{
		// do not send an empty message
		if (string.IsNullOrWhiteSpace(searchEntry.Text))
			return;

		// hide keyboard after sending a message
		await searchEntry.HideSoftInputAsync(CancellationToken.None);

		_ = SearchResultsAsync(SearchQuery, false);
	}

	partial void OnSelectedCategoryChanged(string? value)
	{
		if (!string.IsNullOrEmpty(value))
		{
			_ = SearchResultsAsync(value, true);
			SelectedCategory = null;
		}
	}

	private async Task SearchResultsAsync(string? searchInput, bool isPredefined) => await Shell.Current.GoToAsync($"{nameof(SearchResultPage)}?query={searchInput}&isPredefined={isPredefined}");
}
