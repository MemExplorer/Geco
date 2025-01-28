namespace Geco.Views.Tutorial;

public partial class SearchTutorial : ContentPage
{
	public SearchTutorial()
	{
		InitializeComponent();
	}

	private void btnNextPermission_Clicked(object sender, EventArgs e)
	{
		Shell.Current.GoToAsync(nameof(SettingsTutorial));
	}
}