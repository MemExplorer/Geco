using System.Diagnostics;
using Geco.Core.Database;
using Xunit.Abstractions;

namespace Geco.Core.Test;

public class DatabaseTest
{
	private readonly ITestOutputHelper _output;

	public DatabaseTest(ITestOutputHelper output) => this._output = output;

	[Fact]
	async Task TriggerDatabaseTest()
	{
		string dbPath = Environment.CurrentDirectory;
		var triggerRepo = new TriggerRepository(dbPath);
		await triggerRepo.LogTrigger(DeviceInteractionTrigger.ChargingSustainable, 1);
		await triggerRepo.LogTrigger(DeviceInteractionTrigger.ChargingSustainable, 1);
		await triggerRepo.LogTrigger(DeviceInteractionTrigger.ChargingSustainable, 1);
		await triggerRepo.LogTrigger(DeviceInteractionTrigger.ChargingSustainable, 1);
		await triggerRepo.LogTrigger(DeviceInteractionTrigger.ChargingUnsustainable, 1);
		await triggerRepo.LogTrigger(DeviceInteractionTrigger.ChargingUnsustainable, 1);
		await triggerRepo.LogTrigger(DeviceInteractionTrigger.ChargingUnsustainable, 1);

		var triggerData = await triggerRepo.FetchTriggerRecords();
		Debug.Assert(triggerData.Count == 2);

		foreach (var record in triggerData)
		{
			_output.WriteLine($"Type: {record.Type}, Number of Records: {record.RawValue}");
		}

		bool recentTriggerData = await triggerRepo.IsTriggerInCooldown(DeviceInteractionTrigger.ChargingUnsustainable);
		_output.WriteLine($"Is there recorded action trigger within 3 hours: {recentTriggerData}");

		await triggerRepo.PurgeTriggerData();
		triggerData = await triggerRepo.FetchTriggerRecords();
		Debug.Assert(triggerData.Count == 0);
	}

	[Fact]
	async Task PromptDatabaseTest()
	{
		string dbPath = Environment.CurrentDirectory;
		var promptRepo = new PromptRepository(dbPath);

		string searchUserPrompt = await promptRepo.GetPrompt("What are trending fashion globally");
		_output.WriteLine($"Search User Prompt: {searchUserPrompt}");

		string searchCtgPrompt = await promptRepo.GetPrompt(SearchPredefinedTopic.Energy);
		_output.WriteLine($"Search Predefined Category Prompt: {searchCtgPrompt}");

		string triggerPrompt = await promptRepo.GetPrompt(DeviceInteractionTrigger.BrowserUsageUnsustainable);
		_output.WriteLine($"Trigger Prompt: {triggerPrompt}");

		string sustLikelihoodPrompt = await promptRepo.GetLikelihoodPrompt("16.55%", "current_sustainability_likelihood = (7/10) * (12/20) * (10/16) * (29/46)", "Charging: Total frequency – 10, Frequency Sustainable Charging – 3, Frequency Unsustainable Charging – 7");
		_output.WriteLine($"Likelihood Prompt: {sustLikelihoodPrompt}");
	}
}
