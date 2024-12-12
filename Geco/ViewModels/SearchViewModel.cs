using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Geco.Views;

namespace Geco.ViewModels;
public partial class SearchViewModel : ObservableObject
{
	[ObservableProperty] string? searchQuery;

	[RelayCommand]
	async Task Search()
	{
		await Shell.Current.GoToAsync($"{nameof(SearchResultPage)}?Query={SearchQuery}");
	}
}
