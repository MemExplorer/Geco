using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Geco.Core.Database;
using Geco.Views;
using Geco.Views.Helpers;
using GoogleGeminiSDK;
using Microsoft.Extensions.AI;

namespace Geco.ViewModels;

public partial class WeeklyReportChatViewModel : ObservableObject, IQueryAttributable
{
	const string ListeningMessagePlaceholder = "GECO is listening...";
	const string DefaultEditorPlaceholder = "Message to GECO";
	string _speechToTextResultHolder = string.Empty;

	// Microphone properties
	[ObservableProperty] string _microphoneIcon = IconFont.Microphone;
	[ObservableProperty] bool _isMicrophoneEnabled = true;
	[ObservableProperty] Thickness _microphoneMargin = new(0, 0, 10, 0);

	// Chat Properties
	[ObservableProperty] ObservableCollection<ChatMessage> _chatMessages = [];
	[ObservableProperty] string _editorPlaceHolder = DefaultEditorPlaceholder;
	[ObservableProperty] bool _isChatEnabled;
	bool IsWaitingForResponse { get; set; }
	string? HistoryId { get; set; }
	GeminiChat GeminiClient { get; }
	ISpeechToText SpeechToText { get; }
	Queue<Delegate> NavigationQueue { get; }

	#region FunctionCalls
	[Description("Opens or navigates to the settings.")]
	void NavigateToSettingsPage() =>
		NavigationQueue.Enqueue(async () => { await Shell.Current.GoToAsync(nameof(SettingsPage)); });
	
	[Description("Opens or navigates to the weekly reports page.")]
	void NavigateToWeeklyReportsPage() =>
		NavigationQueue.Enqueue(async () => { await Shell.Current.GoToAsync("//" + nameof(ReportsPage)); });
	
	[Description("Opens or navigates to the sustainable search page.")]
	void NavigateToSearchPage() =>
		NavigationQueue.Enqueue(async () => { await Shell.Current.GoToAsync("//" + nameof(SearchPage)); });
	#endregion

	public WeeklyReportChatViewModel()
	{
		GeminiClient = GlobalContext.Services.GetRequiredService<GeminiChat>();
		GeminiClient.OnChatReceive += async (_, e) =>
			await GeminiClientOnChatReceive(e);
		SpeechToText = GlobalContext.Services.GetRequiredService<ISpeechToText>();
		SpeechToText.RecognitionResultUpdated += SpeechToTextOnRecognitionResultUpdated;
		NavigationQueue = new Queue<Delegate>();
	}

	void SpeechToTextOnRecognitionResultUpdated(object? sender, SpeechToTextRecognitionResultUpdatedEventArgs e) =>
		_speechToTextResultHolder = e.RecognitionResult;

	[RelayCommand]
	async Task MicrophoneClick(Editor chatEditor)
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
			IsChatEnabled = false;
			_speechToTextResultHolder = string.Empty;
			await SpeechToText.StartListenAsync(CultureInfo.CurrentCulture);
			await Task.Delay(1000);
			EditorPlaceHolder = ListeningMessagePlaceholder;
		}
		else
		{
			// Update UI state
			EditorPlaceHolder = DefaultEditorPlaceholder;
			IsMicrophoneEnabled = false;
			await Task.Delay(3000);
			await SpeechToText.StopListenAsync();
			chatEditor.Text += _speechToTextResultHolder;
			IsChatEnabled = true;
		}

		IsMicrophoneEnabled = true;
	}

	internal void ChatTextChanged(string newText)
	{
		if (!IsWaitingForResponse)
			IsChatEnabled = newText.Length != 0;
	}

	/// <summary>
	///     Handles chat send and receive events from Gemini
	/// </summary>
	/// <param name="e">Chat message data</param>
	async Task GeminiClientOnChatReceive(ChatReceiveEventArgs e)
	{
		// append received message to chat UI
		var chatRepo = GlobalContext.Services.GetRequiredService<ChatRepository>();
		if (NavigationQueue.Count > 0)
			e.Message.Text = "The command is successfully executed.";
		ChatMessages.Add(e.Message);

		// save chat message to database
		if (HistoryId != null)
			await chatRepo.AppendChat(HistoryId, e.Message);
	}

	[RelayCommand]
	async Task ChatSend(Editor inputEditor)
	{
		var geminiConfig = GlobalContext.Services.GetRequiredKeyedService<GeminiSettings>(GlobalContext.GeminiChat);
		geminiConfig.Functions = new List<AIFunction>
		{
			AIFunctionFactory.Create(NavigateToSettingsPage), 
			AIFunctionFactory.Create(NavigateToWeeklyReportsPage),
			AIFunctionFactory.Create(NavigateToSearchPage)
		};

		// do not send an empty message
		if (string.IsNullOrWhiteSpace(inputEditor.Text))
			return;

		// hide keyboard after sending a message
		await inputEditor.HideSoftInputAsync(CancellationToken.None);
		string inputContent = inputEditor.Text;

		// set input to empty string after sending a message
		inputEditor.Text = string.Empty;

		try
		{
			IsChatEnabled = false;
			IsWaitingForResponse = true;
			IsMicrophoneEnabled = false;

			// send user message to Gemini and append its response
			await GeminiClient.SendMessage(inputContent, settings: geminiConfig);
		}
		catch (Exception ex)
		{
			GlobalContext.Logger.Error<ChatViewModel>(ex);
		}

		// update chat controls
		IsWaitingForResponse = false;
		IsMicrophoneEnabled = true;
		ChatTextChanged(inputEditor.Text);
		
		while (NavigationQueue.Count > 0)
			NavigationQueue.Dequeue().DynamicInvoke(null);
	}

	public async void ApplyQueryAttributes(IDictionary<string, object> query)
	{
		try
		{
#if ANDROID
			// validate intent action
			var intent = Platform.CurrentActivity?.Intent;
			if (intent?.Action != "GecoWeeklyReportNotif")
				return;

			// after execution, set action to null to avoid repeating chat initialization
			intent.SetAction(null);

#endif
			if (!(query.TryGetValue("historyid", out object? obj) && obj is string historyId))
				return;

			var chatRepo = GlobalContext.Services.GetRequiredService<ChatRepository>();
			var currentGecoConversation = await chatRepo.GetHistoryById(historyId);
			await chatRepo.LoadChats(currentGecoConversation);
			HistoryId = currentGecoConversation.Id;
			ChatMessages = currentGecoConversation.Messages;
			GeminiClient.LoadHistory(currentGecoConversation.Messages);
		}
		catch (Exception e)
		{
			GlobalContext.Logger.Error<WeeklyReportChatViewModel>(e);
		}
	}
}
