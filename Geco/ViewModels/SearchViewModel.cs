using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Geco.Views;

namespace Geco.ViewModels;

public partial class SearchViewModel : ObservableObject
{
	[ObservableProperty] string? _searchQuery;
	[ObservableProperty] string? _selectedCategory;

	[RelayCommand]
	async Task Search(Entry searchEntry)
	{
		// do not send an empty message
		if (string.IsNullOrWhiteSpace(searchEntry.Text))
			return;

		// hide keyboard after sending a message
		await searchEntry.HideSoftInputAsync(CancellationToken.None);

		await SearchResultsAsync(SearchQuery, false);
	}

	[RelayCommand]
	async Task ChipClick(Button btnElement) =>
		await SearchResultsAsync(btnElement.Text, true);

	private async Task SearchResultsAsync(string? searchInput, bool isPredefined)
	{
		await Shell.Current.GoToAsync($"{nameof(SearchResultPage)}?query={searchInput}&isPredefined={isPredefined}");
		SearchQuery = string.Empty;
	}
}
