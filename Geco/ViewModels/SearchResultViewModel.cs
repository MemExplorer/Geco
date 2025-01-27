using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Geco.Core.Brave;
using Geco.Core.Database;
using Geco.Core.Models.ActionObserver;
using GoogleGeminiSDK;

namespace Geco.ViewModels;

public partial class SearchResultViewModel : ObservableObject, IQueryAttributable
{
	const int MaxAiSummaryHeightValue = 180;
	[ObservableProperty] ObservableCollection<WebResultEntry> _searchResults = [];
	[ObservableProperty] string? _searchInput;
	[ObservableProperty] string? _aiOverview;
	[ObservableProperty] bool _isSearching;
	[ObservableProperty] double _maxAiSummaryHeight = MaxAiSummaryHeightValue;
	[ObservableProperty] bool _finalPageReached = true;
	[ObservableProperty] bool _aiSummaryVisible;
	[ObservableProperty] bool _showMoreButtonVisibility;

	bool _isPredefined;
	SearchAPI BraveSearchApi { get; } = GlobalContext.Services.GetRequiredService<SearchAPI>();
	GeminiChat ChatClient { get; } = GlobalContext.Services.GetRequiredService<GeminiChat>();
	uint CurrentPageOffset { get; set; } = 1;
	string? CurrentSearchQuery { get; set; }

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

			var triggerRepo = GlobalContext.Services.GetRequiredService<TriggerRepository>();
			var geminiSearchConfig =
				GlobalContext.Services.GetRequiredKeyedService<GeminiSettings>(GlobalContext.GeminiSearchSummary);

			try
			{
				// Search result settings
				CurrentPageOffset = 1;
				CurrentSearchQuery = unescapeDataString;
				FinalPageReached = true;

				// AI Overview settings
				AiOverview = null;
				AiSummaryVisible = false;
				MaxAiSummaryHeight = MaxAiSummaryHeightValue;
				ShowMoreButtonVisibility = false;

				await triggerRepo.LogTrigger(DeviceInteractionTrigger.BrowserUsageSustainable, 0);

				// run search task on a separate thread
				_ = Task.Run(async () =>
				{
					var braveSearchResult = await BraveSearchApi.Search(unescapeDataString, CurrentPageOffset);

					// Run AI Summary task on a separate thread
					_ = Task.Run(async () =>
					{
						string jsonContent = JsonSerializer.Serialize(braveSearchResult);
						var chatSummaryResponse =
							await ChatClient.SendMessage($"Topic: {unescapeDataString}\nSearch Result: {jsonContent}",
								settings: geminiSearchConfig);
						AiOverview = chatSummaryResponse.Text;
						AiSummaryVisible = true;
						ShowMoreButtonVisibility = true;
					});
					foreach (var searchResult in braveSearchResult)
						SearchResults.Add(searchResult);

					FinalPageReached = false;
				});
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
	void ShowMore()
	{
		MaxAiSummaryHeight = double.PositiveInfinity;
		ShowMoreButtonVisibility = false;
	}

	[RelayCommand]
	async Task LoadMore()
	{
		if (string.IsNullOrEmpty(CurrentSearchQuery))
			return;

		try
		{
			CurrentPageOffset++;
			foreach (var searchResult in await BraveSearchApi.Search(CurrentSearchQuery, CurrentPageOffset))
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
