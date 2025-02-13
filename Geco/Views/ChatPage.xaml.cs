using CommunityToolkit.Maui.Alerts;
using Geco.Core.Database;
using Geco.ViewModels;
using Geco.Views.Helpers;
using Syncfusion.Maui.Toolkit.Chips;

namespace Geco.Views;

public partial class ChatPage : ContentPage
{
	ChatViewModel CurrentViewModel { get; }

	public ChatPage(ChatViewModel vm)
	{
		BindingContext = vm;
		CurrentViewModel = vm;
		InitializeComponent();

		// Create new instance of chat page every time page is loaded
		CurrentViewModel.ListViewComponent = vList;
		var layoutMgr = (LinearItemsLayoutManager2)vList.LayoutManager;
		layoutMgr.OnFinishedLoadingItems += async (s, e) => 
			await OnFinishedLoadingItems(s, e);
		Appearing += async (_, _) =>
			await InitializeChat();
		Unloaded += OnUnloaded;
	}

	private async void OnUnloaded(object? sender, EventArgs e)
	{
		if (BindingContext is IAsyncDisposable ad)
			await ad.DisposeAsync();
	}

	async Task OnFinishedLoadingItems(object? sender, EventArgs e)
	{
		var scrollComponentItems = vList.LayoutManager.ReadOnlyLaidOutItems.Last();
		await vList.ScrollToAsync(0, scrollComponentItems.LeftTop.Y, true);
	}

	internal async Task InitializeChat()
	{
		/*
		 * - Code is executed here every time user visits Chat Page
		 * - Basically, this code mimics the creation of new instance of chat
		 */

		var ctx = (ChatViewModel)BindingContext;
		if (Parent.ClassId == "ChatPage")
		{
			// Create new instance of chat when "Chat" flyout is selected
			ctx.Reset();
		}
		else
		{
			// Load history when the selected flyout is not the "Chat" flyout
			var chatRepo = GlobalContext.Services.GetRequiredService<ChatRepository>();
			var appShellCtx = (AppShellViewModel)Parent.BindingContext;
			var currentHistory = appShellCtx.ChatHistoryList.First(x => x.Id == Parent.ClassId);

			// load conversation data
			vList.LayoutManager.InvalidateLayout();
			await chatRepo.LoadChats(currentHistory);
			ctx.LoadHistory(currentHistory);
		}
	}

	private void ChatEntry_TextChanged(object sender, TextChangedEventArgs e) =>
		CurrentViewModel.ChatTextChanged(e.NewTextValue);

	private void Chip_Clicked(object sender, EventArgs e)
	{
		if (sender is SfChip c)
			CurrentViewModel.ChipClick(c, ChatEntry);
	}

	void WebView_OnNavigating(object? sender, WebNavigatingEventArgs e)
	{
		try
		{
			if (e.Url.StartsWith("https://") || e.Url.StartsWith("http://"))
			{
				e.Cancel = true;
				_ = Utils.OpenBrowserView(e.Url);
			}
		}
		catch (Exception exception)
		{
			GlobalContext.Logger.Error<ChatPage>(exception);
		}
	}

	async void TapGestureRecognizer_OnTapped(object? sender, TappedEventArgs e)
	{
		try
		{
			if (e.Parameter is not WebView wv)
				return;

			string copiedTxt = await wv.EvaluateJavaScriptAsync("document.body.innerText");
			await Clipboard.SetTextAsync(copiedTxt);
			await Toast.Make("Copied text to clipboard.").Show();
		}
		catch (Exception ex)
		{
			GlobalContext.Logger.Error<WeeklyReportChatPage>(ex);
		}
	}
}
