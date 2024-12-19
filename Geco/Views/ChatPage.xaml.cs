using Geco.Core.Database;
using Geco.ViewModels;

namespace Geco.Views;

public partial class ChatPage : ContentPage
{
	IServiceProvider SvcProvider { get; }

	public ChatPage(IServiceProvider sp, ChatViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
		SvcProvider = sp;

		// Create new instance of chat page every time page is loaded
		Appearing += async (_, _) =>
			await InitializeChat();
	}

	async Task InitializeChat()
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
			var chatRepo = SvcProvider.GetService<ChatRepository>();
			var appShellCtx = (AppShellViewModel)Parent.BindingContext;
			var currentHistory = appShellCtx.ChatHistoryList.First(x => x.Id == Parent.ClassId);

			// load conversation data
			await chatRepo!.LoadChats(currentHistory);
			ctx.LoadHistory(currentHistory);
		}
	}
}
