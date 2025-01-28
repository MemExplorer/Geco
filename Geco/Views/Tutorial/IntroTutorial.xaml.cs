namespace Geco.Views.Tutorial;

public partial class IntroTutorial : ContentPage
{
	public IntroTutorial()
	{
		InitializeComponent();
	}

	private void btnNextChat_Clicked(object sender, EventArgs e)
	{
		Shell.Current.GoToAsync(nameof(ChatTutorial));
	}
}