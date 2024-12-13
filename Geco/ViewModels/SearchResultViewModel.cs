using System.Collections.ObjectModel;
using System.Data;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Geco.Models;
using Geco.Views;
using GoogleGeminiSDK;
using GoogleGeminiSDK.Models.Components;
using SchemaType = GoogleGeminiSDK.Models.Components.SchemaType;
using ChatRole = Microsoft.Extensions.AI.ChatRole;

namespace Geco.ViewModels;

[QueryProperty("Query", "Query")]
public partial class SearchResultViewModel : ObservableObject
{
	[ObservableProperty] ObservableCollection<GecoSearchResult> searchResults;
	[ObservableProperty] string? query;
	bool isInitialized = false;

	GeminiChat GeminiClient { get; } = new("API_KEY", "gemini-1.5-flash-latest");

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
						{"title", new Schema(SchemaType.STRING)},
						{"description", new Schema(SchemaType.STRING)} 
					},
				Required: ["title", "description"]
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
		if(e.Message.Role != ChatRole.User)
		{
			string message = e.Message.ToString();

			var resultData = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(message)!;

			if (resultData != null)
			{
				SearchResults.Clear();
				foreach (var item in resultData)
				{
					SearchResults.Add(new GecoSearchResult(item["title"], item["description"]));
				}
			}
		}
	}

	partial void OnQueryChanged(string? value)
	{
		// only run the SendSearch once when query has been passed
		if (!isInitialized)
		{
			SendSearch();
			isInitialized = true; 
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
		SendSearch();
	}

	async void SendSearch()
	{
		if (!string.IsNullOrWhiteSpace(Query))
		{
			await GeminiClient.SendMessage(Query, settings: GeminiConfig);
		}
	}



}
