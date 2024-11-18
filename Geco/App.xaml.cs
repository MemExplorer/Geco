namespace Geco;

public partial class App : Application
{
	public App() => InitializeComponent();

	protected override Window CreateWindow(IActivationState? activationState) =>
		new(new AppShell(activationState!.Context.Services));
}
