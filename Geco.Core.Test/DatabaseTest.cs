using System.Diagnostics;
using Geco.Core.Database;
using Geco.Core.Models.ActionObserver;
using Geco.Core.Models.Prompt;
using Xunit.Abstractions;

namespace Geco.Core.Test;

public class DatabaseTest
{
	private readonly ITestOutputHelper _output;

	public DatabaseTest(ITestOutputHelper output) => _output = output;

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

		var triggerData = await triggerRepo.FetchWeekOneTriggerRecords();
		Debug.Assert(triggerData.Count == 2);

		foreach (var record in triggerData)
		{
			_output.WriteLine($"Type: {record.Key}, Number of Records: {record.Value}");
		}

		bool recentTriggerData = await triggerRepo.IsTriggerInCooldown(DeviceInteractionTrigger.ChargingUnsustainable);
		_output.WriteLine($"Is there recorded action trigger within 3 hours: {recentTriggerData}");

		await triggerRepo.PurgeTriggerData();
		triggerData = await triggerRepo.FetchWeekOneTriggerRecords();
		Debug.Assert(triggerData.Count == 0);
	}

	[Fact]
	async Task PromptDatabaseTest()
	{
		string dbPath = Environment.CurrentDirectory;
		var promptRepo = new PromptRepository(dbPath);

		string triggerPrompt = await promptRepo.GetPrompt(DeviceInteractionTrigger.BrowserUsageUnsustainable);
		_output.WriteLine($"Trigger Prompt: {triggerPrompt}");
	}
}
