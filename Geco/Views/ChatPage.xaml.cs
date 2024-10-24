using Geco.ViewModels;

namespace Geco.Views;

public partial class ChatPage : ContentPage
{
	public ChatPage(ChatViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;

		// Create new instance of chat page every time page is loaded
		Loaded += ChatPage_Loaded;
	}

	private void ChatPage_Loaded(object? sender, EventArgs e)
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
			var appShellCtx = (AppShellViewModel)Parent.BindingContext;
			var currentHistory = appShellCtx.ChatHistoryList.First(x => x.Id == Parent.ClassId);
			ctx.LoadHistory(currentHistory);
		}
	}
}
