using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Geco.Core.Brave;
using Geco.Core.Database;
using Geco.Core.Models.ActionObserver;
using Geco.Views.Helpers;
using GoogleGeminiSDK;

namespace Geco.ViewModels;

public partial class SearchResultViewModel : ObservableObject, IQueryAttributable
{
	const int MaxAiSummaryHeightValue = 180;
	const string ListeningMessagePlaceholder = "GECO is listening...";
	const string DefaultEditorPlaceholder = "Discover something...";
	string _speechToTextResultHolder = string.Empty;
	[ObservableProperty] ObservableCollection<WebResultEntry> _searchResults = [];
	[ObservableProperty] string? _searchInput;
	[ObservableProperty] string? _aiOverview;
	[ObservableProperty] bool _isSearching;
	[ObservableProperty] double _maxAiSummaryHeight = MaxAiSummaryHeightValue;
	[ObservableProperty] bool _finalPageReached = true;
	[ObservableProperty] bool _aiSummaryVisible;
	[ObservableProperty] bool _showMoreButtonVisibility;
	[ObservableProperty] string _searchPlaceholder = DefaultEditorPlaceholder;
	[ObservableProperty] bool _isSearchButtonEnabled;

	// Microphone properties
	[ObservableProperty] string _microphoneIcon = IconFont.Microphone;
	[ObservableProperty] bool _isMicrophoneEnabled = true;
	[ObservableProperty] Thickness _microphoneMargin = new(0, 0, 10, 0);

	SearchAPI BraveSearchApi { get; }
	GeminiChat ChatClient { get; }
	uint CurrentPageOffset { get; set; } = 1;
	string? CurrentSearchQuery { get; set; }
	ISpeechToText SpeechToText { get; }

	public SearchResultViewModel()
	{
		BraveSearchApi = GlobalContext.Services.GetRequiredService<SearchAPI>();
		ChatClient = GlobalContext.Services.GetRequiredService<GeminiChat>();
		SpeechToText = new SpeechToTextImplementation();
		SpeechToText.RecognitionResultCompleted += SpeechToTextOnRecognitionResultCompleted;
		SpeechToText.RecognitionResultUpdated += SpeechToTextOnRecognitionResultUpdated;
	}

	async Task MicrophoneStopListening(bool delay = false)
	{
		// Update UI state
		SearchPlaceholder = DefaultEditorPlaceholder;
		IsMicrophoneEnabled = false;
		if (delay)
			await Task.Delay(2000);

		try
		{
			await SpeechToText.StopListenAsync();
		}
		catch
		{
			// handle unsupported error
		}

		if (_speechToTextResultHolder.Length > 0)
			SearchInput = _speechToTextResultHolder;
	}

	private void SpeechToTextOnRecognitionResultUpdated(object? sender, SpeechToTextRecognitionResultUpdatedEventArgs e)
	{
		_speechToTextResultHolder = e.RecognitionResult;
	}

	private async void SpeechToTextOnRecognitionResultCompleted(object? sender, SpeechToTextRecognitionResultCompletedEventArgs e)
	{
		try
		{
			_speechToTextResultHolder = string.Empty;
			MicrophoneMargin = new Thickness(0, 0, 10, 0);
			MicrophoneIcon = IconFont.Microphone;
			await MicrophoneStopListening();
			if (e.RecognitionResult.IsSuccessful)
				SearchInput += e.RecognitionResult.Text;

			IsMicrophoneEnabled = true;
		}
		catch (Exception ex)
		{
			GlobalContext.Logger.Error<SearchResultViewModel>(ex);
		}
	}

	[RelayCommand]
	async Task MicrophoneClick()
	{
		try
		{
			bool isUseMicrophone = MicrophoneIcon == IconFont.Microphone;
			MicrophoneMargin = new Thickness(0, 0, (int)MicrophoneMargin.Right == 10 ? 5.5 : 10, 0);
			MicrophoneIcon = isUseMicrophone ? IconFont.MicrophoneSlash : IconFont.Microphone;
			if (isUseMicrophone)
			{
				bool isAllowed = await SpeechToText.RequestPermissions();
				if (!isAllowed)
				{
					await Toast.Make("Please grant microphone permission.").Show();
					return;
				}

				if (Connectivity.NetworkAccess != NetworkAccess.Internet)
				{
					await Toast.Make("Internet connection is required").Show();
					return;
				}

				// Update UI state
				IsMicrophoneEnabled = false;
				_speechToTextResultHolder = string.Empty;
				await SpeechToText.StartListenAsync(new SpeechToTextOptions()
				{
					Culture = CultureInfo.CurrentCulture,
					ShouldReportPartialResults = true
				});
				SearchPlaceholder = ListeningMessagePlaceholder;
			}
			else
				await MicrophoneStopListening(true);
		}
		catch (FeatureNotSupportedException)
		{
			MicrophoneMargin = new Thickness(0, 0, 10, 0);
			MicrophoneIcon = IconFont.Microphone;
			await MicrophoneStopListening();
			await Toast.Make("This device does not support the speech-to-text feature.").Show();
		}
		catch (Exception ex)
		{
			GlobalContext.Logger.Error<ChatViewModel>(ex);
		}

		IsMicrophoneEnabled = true;
	}

	internal void SearchTextChanged(string newText) =>
		IsSearchButtonEnabled = newText.Length != 0;

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
		SendSearch();
	}

	async void SendSearch()
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
				SearchResults.Clear();
				CurrentPageOffset = 1;
				CurrentSearchQuery = unescapeDataString;
				FinalPageReached = true;
				IsSearching = true;

				// AI Overview settings
				AiOverview = null;
				AiSummaryVisible = false;
				MaxAiSummaryHeight = MaxAiSummaryHeightValue;
				ShowMoreButtonVisibility = false;

				await triggerRepo.LogTrigger(DeviceInteractionTrigger.BrowserUsageSustainable, 0);

				// run search task on a separate thread
				var dispatcher = Dispatcher.GetForCurrentThread();
				if (dispatcher == null)
					throw new Exception("Dispatcher is null!");

				dispatcher.Dispatch(async void () =>
				{
					try
					{
						var braveSearchResult = await BraveSearchApi.Search(unescapeDataString, CurrentPageOffset);
						// Run AI Summary task on a separate thread
						dispatcher.Dispatch(async void () =>
						{
							try
							{
								string jsonContent = JsonSerializer.Serialize(braveSearchResult);
								var chatSummaryResponse =
									await ChatClient.SendMessage($"Topic: {unescapeDataString}\nSearch Result: {jsonContent}",
										settings: geminiSearchConfig);
								AiOverview = chatSummaryResponse.Text;
								AiSummaryVisible = true;
								ShowMoreButtonVisibility = true;
							}
							catch (Exception ex)
							{
								GlobalContext.Logger.Error<SearchViewModel>(ex);
							}
						});

						foreach (var searchResult in braveSearchResult)
							SearchResults.Add(searchResult);

						IsSearching = false;
						FinalPageReached = false;
					}
					catch (Exception ex)
					{
						GlobalContext.Logger.Error<SearchViewModel>(ex);
					}
				});
			}
			catch (Exception searchEx)
			{
				GlobalContext.Logger.Error<SearchViewModel>(searchEx);
			}
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
		SearchInput = Uri.UnescapeDataString(query["query"].ToString()!);
		SendSearch();
	}
}
