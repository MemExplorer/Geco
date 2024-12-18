using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Geco.Core.Database;
using Geco.Core.Models;
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

	GeminiChat GeminiClient { get; } = new(GecoSecrets.GEMINI_API_KEY, "gemini-1.5-flash-latest");

	GeminiSettings GeminiConfig { get; } = new()
	{
		SystemInstructions =
			"You are Geco, a large language model based on Google Gemini. You are developed by SS Bois. Your response should always be sustainability focused, your tone should be like a search engine, and you should always have 3 responses",
		Conversational = false,
		ResponseMimeType = "application/json",
		ResponseSchema = new Schema(
			SchemaType.ARRAY,
			Items: new Schema(SchemaType.OBJECT,
				Properties: new Dictionary<string, Schema>
				{
					{ "Title", new Schema(SchemaType.STRING) }, { "Description", new Schema(SchemaType.STRING) }
				},
				Required: ["Title", "Description"]
			)
		)
	};

	public SearchResultViewModel()
	{
		_searchResults = [];
		GeminiClient.OnChatReceive += (_, e) =>
			GeminiClientOnChatReceive(e);
	}

	private void GeminiClientOnChatReceive(ChatReceiveEventArgs e)
	{
		if (e.Message.Role == ChatRole.User)
			return;

		string message = e.Message.ToString();

		var results = JsonSerializer.Deserialize<List<GecoSearchResult>>(message);

		if (results == null)
			return;

		SearchResults.Clear();
		foreach (var item in results)
			SearchResults.Add(new GecoSearchResult(item.Title, item.Description));
	}

	[RelayCommand]
	async Task UpdateSearch(Entry searchEntry)
	{
		// do not send an empty message
		if (string.IsNullOrWhiteSpace(searchEntry.Text))
			return;

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

			string? unescapeDataString = Uri.UnescapeDataString(SearchInput);
			var promptRepo = ((AppShell)Shell.Current).SvcProvider.GetService<PromptRepository>()!;

			string prompt;

			if (isPredefined &&
			    Enum.TryParse<SearchPredefinedTopic>(unescapeDataString, out var convertedPredefinedTopic))
				prompt = await promptRepo.GetPrompt(convertedPredefinedTopic);
			else
				prompt = await promptRepo.GetPrompt(unescapeDataString);

			if (!string.IsNullOrEmpty(prompt))
				await GeminiClient.SendMessage(prompt, settings: GeminiConfig);
		}
		catch
		{
			// do nothing
		}
	}

	public void ApplyQueryAttributes(IDictionary<string, object> query)
	{
		string searchInput = HttpUtility.UrlDecode(query["query"].ToString())!;
		SearchInput = RemoveEmojis(searchInput);
		string isPredefinedString = query["isPredefined"].ToString()!;
		_isPredefined = bool.TryParse(isPredefinedString, out bool result) && result;
		SendSearch(_isPredefined);
	}

	private static string RemoveEmojis(string input) =>
		RemoveEmojiPattern().Replace(input, string.Empty);

	[GeneratedRegex(@"[\p{Cs}\p{So}\p{Sm}]")]
	private static partial Regex RemoveEmojiPattern();
}
