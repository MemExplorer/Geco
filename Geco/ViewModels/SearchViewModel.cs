using System.Globalization;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Geco.Views;
using Geco.Views.Helpers;

namespace Geco.ViewModels;

public partial class SearchViewModel : ObservableObject
{
	const string ListeningMessagePlaceholder = "GECO is listening...";
	const string DefaultEditorPlaceholder = "Discover something...";
	string _speechToTextResultHolder = string.Empty;
	[ObservableProperty] string? _searchQuery;
	[ObservableProperty] string? _selectedCategory;
	[ObservableProperty] string _searchPlaceholder = DefaultEditorPlaceholder;
	[ObservableProperty] bool _isSearchButtonEnabled;

	// Microphone properties
	[ObservableProperty] string _microphoneIcon = IconFont.Microphone;
	[ObservableProperty] bool _isMicrophoneEnabled = true;
	[ObservableProperty] Thickness _microphoneMargin = new(0, 0, 10, 0);

	ISpeechToText SpeechToText { get; }

	public SearchViewModel()
	{
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
			SearchQuery = _speechToTextResultHolder;
	}

	private void
		SpeechToTextOnRecognitionResultUpdated(object? sender, SpeechToTextRecognitionResultUpdatedEventArgs e) =>
		_speechToTextResultHolder = e.RecognitionResult;

	private async void SpeechToTextOnRecognitionResultCompleted(object? sender,
		SpeechToTextRecognitionResultCompletedEventArgs e)
	{
		try
		{
			_speechToTextResultHolder = string.Empty;
			MicrophoneMargin = new Thickness(0, 0, 10, 0);
			MicrophoneIcon = IconFont.Microphone;
			await MicrophoneStopListening();
			if (e.RecognitionResult.IsSuccessful)
				SearchQuery += e.RecognitionResult.Text;

			IsMicrophoneEnabled = true;
		}
		catch (Exception ex)
		{
			GlobalContext.Logger.Error<SearchViewModel>(ex);
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
				await SpeechToText.StartListenAsync(new SpeechToTextOptions
				{
					Culture = CultureInfo.CurrentCulture, ShouldReportPartialResults = true
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
	async Task Search(Entry searchEntry)
	{
		// do not send an empty message
		if (string.IsNullOrWhiteSpace(searchEntry.Text))
			return;

		// hide keyboard after sending a message
		await searchEntry.HideSoftInputAsync(CancellationToken.None);

		string escapedQuery = Uri.EscapeDataString(searchEntry.Text);
		await SearchResultsAsync(escapedQuery, false);
	}

	[RelayCommand]
	async Task ChipClick(Button btnElement) =>
		await SearchResultsAsync(btnElement.Text[2..], true);

	private async Task SearchResultsAsync(string? searchInput, bool isPredefined)
	{
		await Shell.Current.GoToAsync($"{nameof(SearchResultPage)}?query={searchInput}&isPredefined={isPredefined}");
		SearchQuery = string.Empty;
	}
}
