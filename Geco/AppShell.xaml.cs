using Geco.Models.Chat;
using Geco.ViewModels;
using Geco.Views;
using System.Collections.Specialized;

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
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            if (e.NewItems == null)
                return;

            var firstItem = (ChatHistory)e.NewItems[0]!;
            var c = new ShellContent() {
                ClassId = firstItem.Id,
                Route = firstItem.Id,
                Title = firstItem.Title,
                Content = SvcProvider.GetService<ChatPage>(),
                Icon = "chatbubble.png"
            };

            ChatHistoryFlyout.Items.Insert(0, c);
            await GoToAsync("//" + firstItem.Id);
        }
    }
}
