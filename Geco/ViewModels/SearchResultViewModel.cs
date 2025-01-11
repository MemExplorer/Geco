using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Geco.Core.Database;
using Geco.Core.Models;
using Geco.Core.Models.ActionObserver;
using Geco.Core.Models.Prompt;
using GoogleGeminiSDK;
using GoogleGeminiSDK.Models.Components;
using ChatRole = Microsoft.Extensions.AI.ChatRole;
using SchemaType = GoogleGeminiSDK.Models.Components.SchemaType;

namespace Geco.ViewModels;

public partial class SearchResultViewModel : ObservableObject, IQueryAttributable
{
	[ObservableProperty] ObservableCollection<GecoSearchResult> _searchResults;
	[ObservableProperty] string? _searchInput;
	bool _isPredefined;
	[ObservableProperty] bool _isSearching;
	GeminiChat GeminiClient { get; }

	public SearchResultViewModel()
	{
		_searchResults = [];
		GeminiClient = GlobalContext.Services.GetRequiredService<GeminiChat>();
		GeminiClient.OnChatReceive += async (_, e) =>
			await GeminiClientOnChatReceive(e);
	}

	private async Task GeminiClientOnChatReceive(ChatReceiveEventArgs e)
	{
		// only accept messages from Gemini
		if (e.Message.Role == ChatRole.User)
			return;

		// deserialize structured response
		string message = e.Message.Text!;
		var results = JsonSerializer.Deserialize<List<GecoSearchResult>>(message);

		if (results == null)
			return;

		// populate search result
		foreach (var item in results)
			SearchResults.Add(new GecoSearchResult(item.Title, item.Description));

		// log usage
		var triggerRepo = GlobalContext.Services.GetRequiredService<TriggerRepository>();
		await triggerRepo.LogTrigger(DeviceInteractionTrigger.BrowserUsageSustainable, 0);

		IsSearching = false;
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
			var promptRepo = GlobalContext.Services.GetRequiredService<PromptRepository>()!;
			var geminiConfig = GlobalContext.Services.GetKeyedService<GeminiSettings>(GlobalContext.GeminiSearch);

			string prompt;
			if (isPredefined &&
				Enum.TryParse<SearchPredefinedTopic>(unescapeDataString, out var convertedPredefinedTopic))
				prompt = await promptRepo.GetPrompt(convertedPredefinedTopic);
			else
				prompt = await promptRepo.GetPrompt(unescapeDataString);

			try
			{
				if (!string.IsNullOrEmpty(prompt))
					await GeminiClient.SendMessage(prompt, settings: geminiConfig);
			}
			catch (Exception geminiException)
			{
				GlobalContext.Logger.Error<SearchViewModel>(geminiException);
			}
		}
		catch (Exception ex)
		{
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
