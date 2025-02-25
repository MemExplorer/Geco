using System.Diagnostics;
using Geco.Core.Database;
using Geco.Core.Models.ActionObserver;
using Xunit.Abstractions;

namespace Geco.Core.Test;

public class PromptBuilderTest
{
	private readonly ITestOutputHelper _output;

	public PromptBuilderTest(ITestOutputHelper output) =>
		_output = output;


	[Fact]
	async Task PromptBuilderTestCase()
	{
		string dbPath = Environment.CurrentDirectory;
		var promptRepo = new PromptRepository(dbPath);

		var weeklyReportNoPrevPrompt = await promptRepo.GetLikelihoodPrompt(30, "", "");
		var weeklyReportWithPrevPrompt = await promptRepo.GetLikelihoodWithHistoryPrompt(30, "", 30, "", "");
		var triggerNotifPrompt = await promptRepo.GetPrompt(DeviceInteractionTrigger.ChargingUnsustainable);

		Debug.Assert(!new string[] {
				"CurrentSustainabilityLikelihood",
				"CurrentLikelihoodComputation",
				"SustainabilityLevel"
			}
			.All(x => weeklyReportWithPrevPrompt.Contains($"{{{x}}}")));

		Debug.Assert(!new string[] {
				"CurrentSustainabilityLikelihood",
				"CurrentLikelihoodComputation",
				"PreviousSustainabilityLikelihood",
				"PreviousLikelihoodComputation",
				"SustainabilityLevel"
			}
			.All(x => weeklyReportNoPrevPrompt.Contains($"{{{x}}}")));

		_output.WriteLine($"Weekly Report: Pass");

		Debug.Assert(!new string[] {
				"ActionTrigger",
				"SustainableBaselineData",
				"StoredPromptRefinement"
			}
			.All(x => triggerNotifPrompt.Contains($"{{{x}}}")));

		_output.WriteLine($"Notification: Pass");
	}
}
