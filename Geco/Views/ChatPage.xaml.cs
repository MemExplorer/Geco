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
        var ctx = (ChatViewModel)BindingContext;
        if (Parent.ClassId == "ChatPage")
        {
            ctx.Reset();
        }
        else
        {
            var appShellCtx = (AppShellViewModel)Parent.BindingContext;
            var currentHistory = appShellCtx.ChatHistoryList.First(x => x.Id == Parent.ClassId);
            ctx.LoadHistory(currentHistory);
        }
    }
}
