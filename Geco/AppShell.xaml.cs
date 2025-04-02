using System.Collections.Specialized;
using Geco.Core.Database;
using Geco.Core.Models.Chat;
using Geco.ViewModels;
using Geco.Views;
using Geco.Views.Helpers;

namespace Geco;

public partial class AppShell : Shell
{
	AppShellViewModel Context { get; }

	public AppShell()
	{
		InitializeComponent();

		// If the user accepts the terms and policy, navigate to the chat page.  
		// Otherwise, display the terms and policy page.  
		MainFlyout.CurrentItem = MainFlyout.Items[!GecoSettings.AcceptedTermsAndPolicy ? 0 : 1];

		Context = (AppShellViewModel)BindingContext;
		Context.ChatHistoryList.CollectionChanged += ChatHistoryList_CollectionChanged;
		Navigated += AppShell_Navigated;
		Loaded += async (_, e) =>
			await AppShell_Loaded(e);

		InitializeRoutes();
	}

	protected override bool OnBackButtonPressed()
	{
		if (this.Navigation.NavigationStack.Count != 1)
			return base.OnBackButtonPressed();

		var dispatchTask = Dispatcher.Dispatch(async () =>
		{
			bool exitApp = await this.DisplayAlert("", "Are you sure you want to exit the application?", "Yes", "No");
			if (exitApp)
				Application.Current?.Quit();
		});

		return true;
	}

	void InitializeRoutes()
	{
		// register routes
		Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
		Routing.RegisterRoute(nameof(SearchResultPage), typeof(SearchResultPage));
		Routing.RegisterRoute(nameof(WeeklyReportChatPage), typeof(WeeklyReportChatPage));
	}

	async Task AppShell_Loaded(EventArgs e)
	{
		// Adding an item to the Context.ChatHistoryList triggers an event that executes code to create 
		// a new flyout item using the details from the chat history entry.
		var chatRepo = GlobalContext.Services.GetRequiredService<ChatRepository>();
		await chatRepo.LoadHistory(Context.ChatHistoryList, HistoryType.DefaultConversation, true);
		Application.Current!.UserAppTheme = GecoSettings.DarkMode ? AppTheme.Dark : AppTheme.Light;
	}

	void AppShell_Navigated(object? sender, ShellNavigatedEventArgs e)
	{
		if (CurrentPage.Parent is ShellContent currShellContent)
		{
			Context.IsChatPage = CurrentPage is ChatPage;
			Context.IsChatInstance = Context.IsChatPage && currShellContent.ClassId != "ChatPage";
		}
		else
		{
			Context.IsChatPage = false;
			Context.IsChatInstance = false;
		}

		// update current page title
		Context.PageTitle = CurrentPage.Title ?? string.Empty;
	}

	void ChatHistoryList_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		// This event is triggered when a user creates a new instance of chat and sends a chat
		if (e.Action == NotifyCollectionChangedAction.Add)
		{
			if (e.NewItems == null)
				return;

			// only one item is added at a time
			var firstItem = (GecoConversation)e.NewItems[0]!;
			var iconSrc = new FontImageSource { FontFamily = "FontAwesome", Glyph = IconFont.MessageLines };
			iconSrc.SetAppThemeColor(FontImageSource.ColorProperty, Color.Parse("#403f3f"), Color.Parse("#c0c0c0"));

			var chatPageTransient = GlobalContext.Services.GetRequiredService<ChatPage>();
			var newChatPage = new ShellContent
			{
				ClassId = firstItem.Id,
				Route = firstItem.Id,
				Title = firstItem.Title,
				Content = chatPageTransient,
				Icon = iconSrc
			};

			// Insert the newest chats at the top
			ChatHistoryFlyout.Items.Insert(0, newChatPage);
		}
		else if (e.Action == NotifyCollectionChangedAction.Remove)
		{
			if (e.OldItems == null)
				return;

			// only one item is removed at a time
			var firstItem = (GecoConversation)e.OldItems[0]!;
			var selectedShell = ChatHistoryFlyout.Items.First(x => x.Route == "IMPL_" + firstItem.Id);
			ChatHistoryFlyout.Items.Remove(selectedShell);
		}
		else if (e.Action == NotifyCollectionChangedAction.Reset)
		{
			ChatHistoryFlyout.Items.Clear();
		}
	}
}
