using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Web;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Geco.Core.Brave;
using Geco.Core.Database;
using Geco.Core.Models.ActionObserver;
using Geco.Core.Models.Prompt;

namespace Geco.ViewModels;

public partial class SearchResultViewModel : ObservableObject, IQueryAttributable
{
	[ObservableProperty] ObservableCollection<WebResultEntry> _searchResults;
	[ObservableProperty] string? _searchInput;
	bool _isPredefined;
	[ObservableProperty] bool _isSearching;
	[ObservableProperty] bool _finalPageReached;
	SearchAPI BraveSearchAPI { get; }
	uint CurrentPageOffset { get; set; }
	string? CurrentSearchQuery { get; set; }

	public SearchResultViewModel()
	{
		_searchResults = [];
		CurrentPageOffset = 1;
		CurrentSearchQuery = null;
		_finalPageReached = false;
		BraveSearchAPI = GlobalContext.Services.GetRequiredService<SearchAPI>();
	}

	[RelayCommand]
	async Task UpdateSearch(Entry searchEntry)
	{
		// do not send an empty message
		if (string.IsNullOrWhiteSpace(searchEntry.Text))
			return;

		IsSearching = true;

		// clear results
		SearchResults.Clear();

		// hide keyboard after sending a message
		await searchEntry.HideSoftInputAsync(CancellationToken.None);
		SendSearch(false);
	}

	async void SendSearch(bool isPredefined)
	{
		try
		{
			if (string.IsNullOrEmpty(SearchInput))
				return;

			string unescapeDataString = Uri.UnescapeDataString(SearchInput);
			if (string.IsNullOrEmpty(unescapeDataString))
				return;

			var promptRepo = GlobalContext.Services.GetRequiredService<PromptRepository>();
			var triggerRepo = GlobalContext.Services.GetRequiredService<TriggerRepository>();

			try
			{
				CurrentPageOffset = 1;
				CurrentSearchQuery = unescapeDataString;
				FinalPageReached = false;
				await triggerRepo.LogTrigger(DeviceInteractionTrigger.BrowserUsageSustainable, 0);
				foreach (var searchResult in await BraveSearchAPI.Search(unescapeDataString, CurrentPageOffset))
					SearchResults.Add(searchResult);
			}
			catch (Exception searchEx)
			{
				GlobalContext.Logger.Error<SearchViewModel>(searchEx);
			}

			IsSearching = false;
		}
		catch (Exception ex)
		{
			GlobalContext.Logger.Error<SearchViewModel>(ex);
		}
	}

	[RelayCommand]
	async Task LoadMore()
	{
		if (string.IsNullOrEmpty(CurrentSearchQuery))
			return;

		try
		{
			CurrentPageOffset++;
			foreach (var searchResult in await BraveSearchAPI.Search(CurrentSearchQuery, CurrentPageOffset))
				SearchResults.Add(searchResult);
		}
		catch (Exception ex)
		{
			// error when last page is reached
			if (ex.Message.Contains("status\":422"))
			{
				// make button invisible
				FinalPageReached = true;
				return; 
			}
			GlobalContext.Logger.Error<SearchViewModel>(ex);
		}
	}

	public void ApplyQueryAttributes(IDictionary<string, object> query)
	{
		string searchInput = HttpUtility.UrlDecode(query["query"].ToString())!;
		SearchInput = RemoveEmojis(searchInput);
		string isPredefinedString = query["isPredefined"].ToString()!;
		_isPredefined = bool.TryParse(isPredefinedString, out bool result) && result;
		IsSearching = true;
		SendSearch(_isPredefined);
	}

	private static string RemoveEmojis(string input) =>
		RemoveEmojiPattern().Replace(input, string.Empty);

	[GeneratedRegex(@"[\p{Cs}\p{So}\p{Sm}]")]
	private static partial Regex RemoveEmojiPattern();
}
