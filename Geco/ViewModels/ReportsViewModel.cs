using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Geco.Core.Database;
using Geco.Core.Models.Chat;
using Geco.Views;

namespace Geco.ViewModels;

public partial class ReportsViewModel : ObservableObject
{
	[ObservableProperty] ObservableCollection<GecoConversation> _weeklyReportHistory = [];

	internal async Task SelectReport(GecoConversation conversation) =>
		await Shell.Current.GoToAsync(nameof(WeeklyReportChatPage),
			new Dictionary<string, object> { { "cdata", conversation } });

	public async Task LoadHistory()
	{
		WeeklyReportHistory.Clear();
		var chatRepo = GlobalContext.Services.GetRequiredService<ChatRepository>();
		await chatRepo.LoadHistory(WeeklyReportHistory, HistoryType.WeeklyReportConversation);
	}
}
