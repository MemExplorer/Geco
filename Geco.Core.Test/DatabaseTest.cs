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
}
