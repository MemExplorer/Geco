using Xunit.Abstractions;

namespace Geco.Core.Test;
public class PromptTest
{
	private readonly ITestOutputHelper _output;

	public PromptTest(ITestOutputHelper output) => _output = output;

	[Fact]
	void PromptFillTest()
	{
		var promptManager = new PromptManager();

		string searchUserPrompt = promptManager.GetSearchUserBasedPrompt("What is a known sustainable fashion here in the Philippines?");
		_output.WriteLine($"Sustainable Search User-based: {searchUserPrompt}");

		string searchCategoryPrompt = promptManager.GetSearchCtgBasedPrompt("Sustainable Fashion", "Affordability and Practicality");
		_output.WriteLine($"Sustainable Search Category-based: {searchCategoryPrompt}");

		string triggerNotificationPrompt = promptManager.GetTriggerNotifPrompt("Charging", "Let your battery naturally deplete to around 20% before charging to about 80%", "Recommendations to avoid overstepping the sustainable baseline data");
		_output.WriteLine($"Trigger Notifation: {triggerNotificationPrompt}");

		string likelihoodPrompt = promptManager.GetSustLikelihoodPrompt("16.55%", "current_sustainability_likelihood = (7/10) * (12/20) * (10/16) * (29/46)", "Charging: Total frequency – 10, Frequency Sustainable Charging – 3, Frequency Unsustainable Charging – 7");
		_output.WriteLine($"Sustainability Likelihood: {likelihoodPrompt}");
	}
}
