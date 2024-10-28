using System.Collections.Specialized;
using Geco.Core.Database;
using Geco.Core.Gemini;
using Geco.ViewModels;
using Geco.Views;

namespace Geco;

public partial class AppShell : Shell
{
	private AppShellViewModel Context { get; }
	internal IServiceProvider SvcProvider { get; }
	public AppShell(IServiceProvider provider)
	{
		InitializeComponent();

		SvcProvider = provider;
		Context = (AppShellViewModel)BindingContext;
		Context.ChatHistoryList.CollectionChanged += ChatHistoryList_CollectionChanged;
		Navigated += AppShell_Navigated;

		// register routes
		Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));

		// Adding an item to the Context.ChatHistoryList triggers an event that executes code to create 
		// a new flyout item using the details from the chat history entry.
		var chatRepo = SvcProvider.GetService<ChatRepository>();
		chatRepo!.LoadHistory(Context.ChatHistoryList).Wait();
	}

	private void AppShell_Navigated(object? sender, ShellNavigatedEventArgs e)
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
		Context.PageTitle = CurrentPage.Title;
	}

	private void ChatHistoryList_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		// This event is triggered when a user creates a new instance of chat and sends a chat
		if (e.Action == NotifyCollectionChangedAction.Add)
		{
			if (e.NewItems == null)
				return;

			// only one item is added at a time
			var firstItem = (ChatHistory)e.NewItems[0]!;
			var newChatPage = new ShellContent()
			{
				ClassId = firstItem.Id,
				Route = firstItem.Id,
				Title = firstItem.Title,
				Content = SvcProvider.GetService<ChatPage>(),
				Icon = "chatbubble.png"
			};

			// Insert newest chats at the top
			ChatHistoryFlyout.Items.Insert(0, newChatPage);
		}
		else if (e.Action == NotifyCollectionChangedAction.Remove)
		{
			if (e.OldItems == null)
				return;

			// only one item is removed at a time
			var firstItem = (ChatHistory)e.OldItems[0]!;
			var selectedShell = ChatHistoryFlyout.Items.First(x => x.Route == "IMPL_" + firstItem.Id);
			ChatHistoryFlyout.Items.Remove(selectedShell);
		}
	}
}
