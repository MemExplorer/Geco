using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Geco.Views;

namespace Geco.ViewModels;
public partial class SearchViewModel : ObservableObject
{
	[ObservableProperty] string? searchQuery;

	[RelayCommand]
	async Task Search(Entry searchEntry)
	{
		// do not send an empty message
		if (string.IsNullOrWhiteSpace(searchEntry.Text))
			return;

		// hide keyboard after sending a message
		await searchEntry.HideSoftInputAsync(CancellationToken.None);

		await Shell.Current.GoToAsync($"{nameof(SearchResultPage)}?Query={SearchQuery}");
	}
}
