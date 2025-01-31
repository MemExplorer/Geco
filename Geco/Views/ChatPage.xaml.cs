using Geco.Core.Database;
using Geco.ViewModels;
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
		Appearing += async (_, _) =>
			await InitializeChat();
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

	private async void WebView_Navigated(object sender, WebNavigatedEventArgs e)
	{
		try
		{
			if (sender is not WebView w)
				return;

			string backgroundColor = GecoSettings.DarkMode ? "#1C1C1C" : "#FFFFFF";
			string textColor = GecoSettings.DarkMode ? "#ffffff" : "#000000";

			// load md to html converter script
			await using var stream = await FileSystem.OpenAppPackageFileAsync("showdown.min.js");
			using var reader = new StreamReader(stream);
			string? showdownJs = await reader.ReadToEndAsync();

			await w.EvaluateJavaScriptAsync($$"""
			                                  {{showdownJs}}
			                                  var converter = new showdown.Converter();
			                                  converter.setOption('tables', true);
			                                  converter.setOption('simpleLineBreaks', true);
			                                  converter.setOption('requireSpaceBeforeHeadingText', true);
			                                  converter.setOption('simplifiedAutoLink', true);
			                                  const contentElement = document.getElementById('gecocontent');
			                                  contentElement.innerHTML = converter.makeHtml(contentElement.innerHTML);
			                                  (function() {
			                                  	function modifyStyles(backgroundColor, textColor) {
			                                  		document.body.style.backgroundColor = backgroundColor; 
			                                  		document.body.style.color = textColor; 
			                                  	}
			                                  
			                                  	modifyStyles('{{backgroundColor}}', '{{textColor}}');
			                                  })();
			                                  """);
		}
		catch (Exception ex)
		{
			GlobalContext.Logger.Error<ChatPage>(ex);
		}
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
			else if (!e.Url.StartsWith("data:text/html;base64,"))
				e.Cancel = true;
		}
		catch (Exception exception)
		{
			GlobalContext.Logger.Error<ChatPage>(exception);
		}
	}
}
