using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Geco.Models;
using Geco.Views;

namespace Geco.ViewModels;

[QueryProperty("Query", "Query")]
public partial class SearchResultViewModel : ObservableObject
{
	[ObservableProperty] ObservableCollection<GecoSearchResult> searchResults;
	[ObservableProperty]
	string? query;

	[RelayCommand]
	async Task UpdateSearch()
	{
		await Shell.Current.GoToAsync($"{nameof(SearchResultPage)}?Query={Query}");
	}
	public SearchResultViewModel()
	{ 
		searchResults = new ObservableCollection<GecoSearchResult>();
		//Search Results Below
		//SearchResults.Add(new GecoSearchResult("Title", "Description"));
	}

}
