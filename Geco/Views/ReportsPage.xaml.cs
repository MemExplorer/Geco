using Geco.Core.Database;
using Geco.Core.Models.Chat;
using Geco.ViewModels;

namespace Geco.Views;

public partial class ReportsPage : ContentPage
{
	ReportsViewModel ViewModel { get; }

	public ReportsPage()
	{
		InitializeComponent();
		ViewModel = (ReportsViewModel)BindingContext;
		Appearing += async (_, _) => await OnAppearingPage();
	}

	async Task OnAppearingPage() => await ViewModel.LoadHistory();

	async void TapGestureRecognizer_OnTapped(object? sender, TappedEventArgs e)
	{
		try
		{
			if (e.Parameter is not GecoConversation gc)
				return;

			if (sender is Label)
			{
				var currentShell = (AppShell)Shell.Current;
				bool deleteAns =
					await currentShell.DisplayAlert("",
						"Are you sure you want to delete this weekly report conversation?", "Yes", "No");

				if (!deleteAns)
					return;

				var chatRepo = GlobalContext.Services.GetRequiredService<ChatRepository>();
				ViewModel.WeeklyReportHistory.Remove(gc);
				await chatRepo.DeleteHistory(gc.Id);
			}
			else
			{
				await ViewModel.SelectReport(gc);
			}
		}
		catch (Exception ex)
		{
			GlobalContext.Logger.Error<ReportsPage>(ex);
		}
	}
}
