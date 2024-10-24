using System.Collections.Specialized;
using Geco.Models.Chat;
using Geco.ViewModels;
using Geco.Views;

namespace Geco;

public partial class AppShell : Shell
{
	private IServiceProvider SvcProvider { get; }
	public AppShell(IServiceProvider provider)
	{
		InitializeComponent();
		SvcProvider = provider;
		var ctx = (AppShellViewModel)BindingContext;
		ctx.ChatHistoryList.CollectionChanged += ChatHistoryList_CollectionChanged;
	}

	private async void ChatHistoryList_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		// This event is triggered when a user creates a new instace of chat and sends a chat
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

			// redirect user to the new chat page
			await GoToAsync("//" + firstItem.Id);
		}
	}
}
