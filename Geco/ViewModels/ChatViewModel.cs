using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Geco.Core.Database;
using Geco.Core.Models.Chat;
using Geco.Views;
using Geco.Views.Helpers;
using GoogleGeminiSDK;
using Microsoft.Extensions.AI;
using MPowerKit.VirtualizeListView;
using Syncfusion.Maui.Toolkit.Chips;

namespace Geco.ViewModels;

public partial class ChatViewModel : ObservableObject, IAsyncDisposable
{
	#region Fields

	const string ListeningMessagePlaceholder = "GECO is listening...";
	const string DefaultEditorPlaceholder = "Message to GECO";

	// Microphone properties
	[ObservableProperty] string _microphoneIcon = IconFont.Microphone;
	[ObservableProperty] bool _isMicrophoneEnabled = true;
	[ObservableProperty] Thickness _microphoneMargin = new(0, 0, 10, 0);
	string _speechToTextResultHolder = string.Empty;

	// Chat properties
	[ObservableProperty] ObservableCollection<ChatMessage> _chatMessages = [];
	[ObservableProperty] string _editorPlaceHolder = DefaultEditorPlaceholder;
	[ObservableProperty] string _editorTextContent = string.Empty;
	[ObservableProperty] bool _isChatEnabled;
	[ObservableProperty] bool _isAutoCompleteVisible = true;
	internal VirtualizeListView? ListViewComponent { get; set; }
	bool IsWaitingForResponse { get; set; }
	Queue<Delegate> NavigationQueue { get; }

	// Services
	GeminiChat GeminiClient { get; }
	ISpeechToText SpeechToText { get; }

	// Chat configuration
	string? HistoryId { get; set; }
	string? ActionTitle { get; set; }

	#endregion

	public ChatViewModel()
	{
		GeminiClient = GlobalContext.Services.GetRequiredService<GeminiChat>();
		SpeechToText = new SpeechToTextImplementation();
		SpeechToText.RecognitionResultCompleted += SpeechToTextOnRecognitionResultCompleted;
		SpeechToText.RecognitionResultUpdated += SpeechToTextOnRecognitionResultUpdated;
		GeminiClient.OnChatReceive += async (_, e) =>
			await GeminiClientOnChatReceive(e);
		NavigationQueue = new Queue<Delegate>();
	}

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

	#region Controllers

	async Task MicrophoneStopListening(bool delay = false)
	{
		// Update UI state
		EditorPlaceHolder = DefaultEditorPlaceholder;
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
			EditorTextContent = _speechToTextResultHolder;
		IsChatEnabled = true;
	}

	/// <summary>
	///     Loads chat history into the ChatViewModel instance
	/// </summary>
	/// <param name="conversationInfo">Conversation data</param>
	public void LoadHistory(GecoConversation conversationInfo)
	{
		ChatMessages = conversationInfo.Messages;
		GeminiClient.LoadHistory(conversationInfo.Messages);
		HistoryId = conversationInfo.Id;
	}

	/// <summary>
	///     Resets current chat instance
	/// </summary>
	public void Reset()
	{
		ChatMessages = [];
		GeminiClient.ClearHistory();
		HistoryId = null;

#if ANDROID
		// handle notification message
		var intent = Platform.CurrentActivity?.Intent;
		if (intent?.Action != "GecoNotif")
			return;

		string? msgContent = intent.GetStringExtra("message");
		ActionTitle = intent.GetStringExtra("title");
		var chatMsg = new ChatMessage(new ChatRole("model"), msgContent);
		chatMsg.AdditionalProperties = new AdditionalPropertiesDictionary { ["id"] = (ulong)0 };
		ChatMessages.Add(chatMsg);
		intent.SetAction(null);
#endif
	}

	#endregion

	#region Event Handlers

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

		// Scroll to bottom effect after sending a message
		if (ListViewComponent != null)
		{
			var scrollComponentItems = ListViewComponent.LayoutManager.ReadOnlyLaidOutItems.Last();
			await ListViewComponent.ScrollToAsync(0, scrollComponentItems.LeftTop.Y, true);
		}
	}

	internal void ChatTextChanged(string newText)
	{
		if (!IsAutoCompleteVisible && newText.Length == 0)
			IsAutoCompleteVisible = true;

		if (!IsWaitingForResponse)
			IsChatEnabled = newText.Length != 0;
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
				EditorTextContent += e.RecognitionResult.Text;

			IsMicrophoneEnabled = true;
		}
		catch (Exception ex)
		{
			GlobalContext.Logger.Error<ChatViewModel>(ex);
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
				IsChatEnabled = false;
				_speechToTextResultHolder = string.Empty;
				await SpeechToText.StartListenAsync(new SpeechToTextOptions
				{
					Culture = CultureInfo.CurrentCulture, ShouldReportPartialResults = true
				});
				EditorPlaceHolder = ListeningMessagePlaceholder;
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

	internal void ChipClick(SfChip chip, Editor chatEditor)
	{
		IsAutoCompleteVisible = false;
		chatEditor.Text = chip.Text switch
		{
			"Impacts of fast fashion" => "Can you tell me what are the impacts of the fast fashion to the environment?",
			"Surprise me" => "Surprise me with anything about sustainability.",
			"Sustainability Advice" => "Can you give me some advice related to being more sustainable?",
			"Tutorial" => "Can you teach me how to use this application?",
			_ => chatEditor.Text
		};
	}

	[RelayCommand]
	async Task ChatSend(Editor inputEditor)
	{
		var currentShell = (AppShell)Shell.Current;
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
		bool isNewChat = await InitializeNewConversation(currentShell, inputContent);

		// set input to empty string after sending a message
		inputEditor.Text = string.Empty;

		try
		{
			IsChatEnabled = false;
			IsMicrophoneEnabled = false;
			IsWaitingForResponse = true;

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

		if (isNewChat)
			await currentShell.GoToAsync("//" + HistoryId);

		while (NavigationQueue.Count > 0)
			NavigationQueue.Dequeue().DynamicInvoke(null);
	}

	#endregion

	#region Utilities

	async Task<bool> InitializeNewConversation(AppShell currentShell, string inputContent)
	{
		// saves new instance of a chat
		bool gecoInitiated = ChatMessages.Count == 1 && ChatMessages.First().Role != ChatRole.User;
		bool isNewChat = (ChatMessages.Count == 0 || gecoInitiated) && HistoryId == null;
		if (isNewChat)
		{
			var chatRepo = GlobalContext.Services.GetRequiredService<ChatRepository>();
			var shellViewModel = (AppShellViewModel)currentShell.BindingContext;
			string chatTitle = CreateChatTitle(gecoInitiated ? ActionTitle! : inputContent);
			var historyInstance = new GecoConversation(Guid.NewGuid().ToString(), HistoryType.DefaultConversation,
				chatTitle,
				DateTimeOffset.UtcNow.ToUnixTimeSeconds(), ChatMessages);

			// append to UI
			shellViewModel.ChatHistoryList.Add(historyInstance);

			// save to database
			await chatRepo.AppendHistory(historyInstance);

			// set new history id
			HistoryId = historyInstance.Id;

			if (gecoInitiated)
				GeminiClient.AppendToHistory(ChatMessages.First());
		}

		return isNewChat;
	}

	static string CreateChatTitle(string message)
	{
		// For now, I think 17 is a good max length for a title
		const uint maxTitleLen = 17;
		if (message.Length <= maxTitleLen)
			return message.Trim() + "...";
		return message[..17].Trim() + "...";
	}

	public async ValueTask DisposeAsync() =>
		await SpeechToText.DisposeAsync();

	#endregion
}
