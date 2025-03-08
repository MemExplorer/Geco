using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Geco.Core.Database;
using Geco.Core.Models.Chat;
using Geco.Views;

namespace Geco.ViewModels;

public partial class ReportsViewModel : ObservableObject
{
	[ObservableProperty] ObservableCollection<GecoConversation> _weeklyReportHistory = [];
	[ObservableProperty] bool _isTipVisible = GecoSettings.WeeklyReportTipVisible;

	internal async Task SelectReport(GecoConversation conversation)
	{
#if ANDROID
		Platform.CurrentActivity?.Intent?.SetAction("GecoWeeklyReportNotif");
#endif

		await Shell.Current.GoToAsync(nameof(WeeklyReportChatPage),
			new Dictionary<string, object> { { "historyid", conversation.Id } });
	}

	[RelayCommand]
	void DismissTip()
	{
		IsTipVisible = false;
		GecoSettings.WeeklyReportTipVisible = false;
	}

	public async Task LoadHistory()
	{
		WeeklyReportHistory.Clear();
		var chatRepo = GlobalContext.Services.GetRequiredService<ChatRepository>();
		await chatRepo.LoadHistory(WeeklyReportHistory, HistoryType.WeeklyReportConversation, false);
	}
}
