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
		Context = (AppShellViewModel)BindingContext;
		Context.ChatHistoryList.CollectionChanged += ChatHistoryList_CollectionChanged;
		Navigated += AppShell_Navigated;
		Loaded += async (_, e) =>
			await AppShell_Loaded(e);

		InitializeRoutes();
	}

	void InitializeRoutes()
	{
		// register routes
		Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
		Routing.RegisterRoute(nameof(SearchResultPage), typeof(SearchResultPage));
	}

	async Task AppShell_Loaded(EventArgs e)
	{
		// Adding an item to the Context.ChatHistoryList triggers an event that executes code to create 
		// a new flyout item using the details from the chat history entry.
		var chatRepo = GlobalContext.Services.GetRequiredService<ChatRepository>();
		await chatRepo.LoadHistory(Context.ChatHistoryList, HistoryType.DefaultConversation);
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
			iconSrc.SetAppThemeColor(FontImageSource.ColorProperty, Color.Parse("#262626"), Color.Parse("#D3D3D3"));

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
