using Geco.Core.Database;
using Geco.ViewModels;
using Syncfusion.Maui.Toolkit.Chips;

namespace Geco.Views;

public partial class ChatPage : ContentPage
{
	ChatViewModel CurrentViewModel { get; init; }

	public ChatPage(ChatViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
		CurrentViewModel = vm;

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
			var chatRepo = GlobalContext.Services.GetRequiredService<ChatRepository>();
			var appShellCtx = (AppShellViewModel)Parent.BindingContext;
			var currentHistory = appShellCtx.ChatHistoryList.First(x => x.Id == Parent.ClassId);

			// load conversation data
			await chatRepo!.LoadChats(currentHistory);
			ctx.LoadHistory(currentHistory);
		}
	}

	private void ChatEntry_TextChanged(object sender, TextChangedEventArgs e) =>
		CurrentViewModel.ChatTextChanged(e);

	private void Chip_Clicked(object sender, EventArgs e)
	{
		if (sender is SfChip c)
			CurrentViewModel.ChipClick(c, ChatEntry);
	}
		
}
