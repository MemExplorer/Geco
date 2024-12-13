using System.Diagnostics;
using Geco.Core.Database;

namespace Geco.Core.Test;

public class DatabaseTest
{

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
		await triggerRepo.PurgeTriggerData();
		triggerData = await triggerRepo.FetchTriggerRecords();
		Debug.Assert(triggerData.Count == 0);
	}

	[Fact]
	async Task GeminiTest()
	{

	}
}
