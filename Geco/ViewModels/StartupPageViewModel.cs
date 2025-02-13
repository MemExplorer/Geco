using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Geco.ViewModels;

public partial class StartupPageViewModel : ObservableObject
{
	[RelayCommand]
	async Task Continue()
	{
		GecoSettings.AcceptedTermsAndPolicy = true;
		await Shell.Current.GoToAsync("//ChatPage", true);
	}

	[RelayCommand]
	void LearnMoreAboutReviewers()
	{
#if ANDROID
		Utils.OpenExternalBrowserView("https://support.google.com/gemini?p=privacy_help#reviewers");
#endif
	}

	[RelayCommand]
	void OpenGoogleTerms()
	{
#if ANDROID
		Utils.OpenExternalBrowserView("https://policies.google.com/terms");
#endif
	}


	[RelayCommand]
	void OpenGooglePrivacy()
	{
#if ANDROID
		Utils.OpenExternalBrowserView("https://support.google.com/gemini?p=privacy_help#reviewers");
#endif
	}

	[RelayCommand]
	void OpenBraveSearchPrivacy()
	{
#if ANDROID
		Utils.OpenExternalBrowserView("https://search.brave.com/help/privacy-policy");
#endif
	}
}
