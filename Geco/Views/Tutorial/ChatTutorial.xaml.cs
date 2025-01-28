namespace Geco.Views.Tutorial;

public partial class ChatTutorial : ContentPage
{
	public ChatTutorial()
	{
		InitializeComponent();
	}

	private void btnNextSearch_Clicked(object sender, EventArgs e)
	{
		Shell.Current.GoToAsync(nameof(SearchTutorial));
	}
}