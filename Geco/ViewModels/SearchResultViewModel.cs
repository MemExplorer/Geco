using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Geco.Core.Database;
using Geco.Models;
using GoogleGeminiSDK;
using GoogleGeminiSDK.Models.Components;
using ChatRole = Microsoft.Extensions.AI.ChatRole;
using SchemaType = GoogleGeminiSDK.Models.Components.SchemaType;

namespace Geco.ViewModels;

public partial class SearchResultViewModel : ObservableObject, IQueryAttributable
{
	[ObservableProperty] ObservableCollection<GecoSearchResult> searchResults;
	[ObservableProperty] string? searchInput;
	[ObservableProperty] bool isShimmerVisible = true, isResultsVisible = false;
	bool isPredefined;

	GeminiChat GeminiClient { get; } = new(GecoSecrets.GEMINI_API_KEY, "gemini-1.5-flash-latest");

	GeminiSettings GeminiConfig { get; } = new()
	{
		SystemInstructions =
			"You are Geco, a large language model based on Google Gemini. You are developed by SS Bois. Your response should always be sustainability focused, your tone should be like a search engine, and you should always have 3 responses",
		ResponseMimeType = "application/json",
		ResponseSchema = new Schema(
			SchemaType.ARRAY,
			Items: new Schema(SchemaType.OBJECT,
				Properties: new Dictionary<string, Schema>
					{
						{"Title", new Schema(SchemaType.STRING)},
						{"Description", new Schema(SchemaType.STRING)}
					},
				Required: ["Title", "Description"]
				)
			)
	};

	public SearchResultViewModel()
	{
		searchResults = [];
		GeminiClient.OnChatReceive += (_, e) =>
			GeminiClientOnChatReceive(e);
	}

	private void GeminiClientOnChatReceive(ChatReceiveEventArgs e)
	{
		if (e.Message.Role != ChatRole.User)
		{
			string message = e.Message.ToString();

			var results = JsonSerializer.Deserialize<List<GecoSearchResult>>(message);

			if (results != null)
			{
				SearchResults.Clear();
				IsShimmerVisible = false;
				IsResultsVisible = true;
				foreach (var item in results)
				{
					SearchResults.Add(new GecoSearchResult(item.Title, item.Description));
				}
			}
		}
	}

	[RelayCommand]
	async Task UpdateSearch(Entry searchEntry)
	{
		// do not send an empty message
		if (string.IsNullOrWhiteSpace(searchEntry.Text))
			return;

		// hide keyboard after sending a message
		await searchEntry.HideSoftInputAsync(CancellationToken.None);

		IsShimmerVisible = true;
		IsResultsVisible = false;

		SendSearch(false);
	}

	async void SendSearch(bool isPredefined)
	{
		if (!string.IsNullOrEmpty(SearchInput))
		{
			var searchInput = Uri.UnescapeDataString(SearchInput);
			var promptRepo = ((AppShell)Shell.Current).SvcProvider.GetService<PromptRepository>();

			string? prompt = null;

			if (promptRepo != null)
			{
				if (isPredefined && Enum.TryParse<SearchPredefinedTopic>(searchInput, out var convertedPredefTopic))
				{
					prompt = await promptRepo.GetPrompt(predefinedTopic: convertedPredefTopic);
					Console.WriteLine(convertedPredefTopic);
				}
				else
				{
					prompt = await promptRepo.GetPrompt(userTopic: searchInput);
				}
			}

			if (!string.IsNullOrEmpty(prompt))
			{
				await GeminiClient.SendMessage(message: prompt, settings: GeminiConfig);
			}
		}
	}

	public void ApplyQueryAttributes(IDictionary<string, object> query)
	{
		string searchInput = HttpUtility.UrlDecode(query["query"].ToString())!;
		SearchInput = RemoveEmojis(searchInput);

		string isPredefinedString = query["isPredefined"].ToString()!;

		isPredefined = bool.TryParse(isPredefinedString, out bool result) && result;

		if (isPredefined)
			SendSearch(true);
		else
			SendSearch(false);
	}

	public static string RemoveEmojis(string input)
	{
		string pattern = @"[\p{Cs}\p{So}\p{Sm}]";

		return Regex.Replace(input, pattern, string.Empty);
	}
}
