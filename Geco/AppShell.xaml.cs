using System.Collections.Specialized;
using Geco.Core.Database;
using Geco.Core.Gemini;
using Geco.ViewModels;
using Geco.Views;

namespace Geco;

public partial class AppShell : Shell
{
	internal IServiceProvider SvcProvider { get; }
	public AppShell(IServiceProvider provider)
	{
		InitializeComponent();
		SvcProvider = provider;
		var ctx = (AppShellViewModel)BindingContext;
		ctx.ChatHistoryList.CollectionChanged += ChatHistoryList_CollectionChanged;

		// Adding an item to the ctx.ChatHistoryList triggers an event that executes code to create 
		// a new flyout item using the details from the chat history entry.
		var chatRepo = SvcProvider.GetService<ChatRepository>();
		chatRepo!.LoadHistory(ctx.ChatHistoryList).Wait();
	}

	private void ChatHistoryList_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		// This event is triggered when a user creates a new instance of chat and sends a chat
		if (e.Action == NotifyCollectionChangedAction.Add)
		{
			if (e.NewItems == null)
				return;

			// only one item is added
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
	}
}
