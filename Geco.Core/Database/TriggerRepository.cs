using Geco.Core.Database.SqliteModel;
using Geco.Core.Models.ActionObserver;

namespace Geco.Core.Database;

public class TriggerRepository : DbRepositoryBase
{
	const long TwoWeeksInSeconds = 1_209_600;
	const long OneWeekInSeconds = 604_800;
	const long ThreeHoursInSeconds = 10_800;

	public TriggerRepository(string databaseDir) : base(databaseDir)
	{
	}

	// Database table blueprint
	internal override TblSchema[] TableSchemas =>
	[
		new("TblTriggerLog", [
			new TblField("Timestamp", TblFieldType.Integer),
			new TblField("Type", TblFieldType.Integer),
			new TblField("RawValue", TblFieldType.Integer)
		])
	];

	public async Task LogTrigger(DeviceInteractionTrigger triggerType, int rawValue)
	{
		await Initialize();

		using var db = await SqliteDb.GetTransient(DatabaseDir);
		await db.ExecuteNonQuery("INSERT INTO TblTriggerLog VALUES (unixepoch(), ?, ?)", (int)triggerType, rawValue);
	}

	public async Task<IDictionary<DeviceInteractionTrigger, int>> FetchWeekOneTriggerRecords()
	{
		await Initialize();

		using var db = await SqliteDb.GetTransient(DatabaseDir);

		// ensure that the fetched data corresponds to records from the last 7 days.
		var triggers = new Dictionary<DeviceInteractionTrigger, int>();
		await using var fetchQuery = await db.ExecuteReader(
			"SELECT Type, SUM(RawValue) FROM TblTriggerLog WHERE (unixepoch() - Timestamp) <= ? GROUP BY Type",
			OneWeekInSeconds);
		while (fetchQuery.Read())
			triggers.Add((DeviceInteractionTrigger)(long)fetchQuery["Type"],
				(int)(long)fetchQuery["SUM(RawValue)"]);

		return triggers;
	}

	public async Task<IDictionary<DeviceInteractionTrigger, int>> FetchWeekTwoTriggerRecords()
	{
		await Initialize();

		using var db = await SqliteDb.GetTransient(DatabaseDir);

		// ensure that the fetched data corresponds to records from the last 2 weeks.
		var triggers = new Dictionary<DeviceInteractionTrigger, int>();
		await using var fetchQuery = await db.ExecuteReader(
			"SELECT Type, SUM(RawValue) FROM TblTriggerLog WHERE (unixepoch() - Timestamp) > ? AND (unixepoch() - Timestamp) <= ?) GROUP BY Type",
			OneWeekInSeconds, TwoWeeksInSeconds);
		while (fetchQuery.Read())
			triggers.Add((DeviceInteractionTrigger)(long)fetchQuery["Type"],
				(int)(long)fetchQuery["SUM(RawValue)"]);

		return triggers;
	}

	public async Task<bool> HasHistory()
	{
		await Initialize();

		using var db = await SqliteDb.GetTransient(DatabaseDir);

		// check whether we have data that exists for more than a week
		long entryCount =
			await db.ExecuteScalar<long>("SELECT 1 FROM TblTriggerLog WHERE (unixepoch() - Timestamp) > ?",
				OneWeekInSeconds);
		return entryCount == 1;
	}

	public async Task<bool> IsTriggerInCooldown(DeviceInteractionTrigger interactionTrigger, int? cooldownInSeconds = null)
	{
		await Initialize();

		using var db = await SqliteDb.GetTransient(DatabaseDir);

		// check if there is a record of the specified interaction trigger within 3 hours.
		return await db.ExecuteScalar<long>(
				   $"SELECT EXISTS (SELECT 1 FROM TblTriggerLog WHERE (Type = {(int)interactionTrigger} OR Type = {-(int)interactionTrigger}) AND (unixepoch() - Timestamp) <= ?)",
				   cooldownInSeconds ?? ThreeHoursInSeconds) == 1;
	}

	public async Task PurgeLastTwoWeeksTriggerData()
	{
		await Initialize();

		using var db = await SqliteDb.GetTransient(DatabaseDir);

		// only delete data that's been in the database for more than 14 days
		await db.ExecuteNonQuery("DELETE FROM TblTriggerLog WHERE (unixepoch() - Timestamp) > ?", TwoWeeksInSeconds);
	}

	public async Task PurgeTriggerData()
	{
		await Initialize();

		using var db = await SqliteDb.GetTransient(DatabaseDir);
		await db.ExecuteNonQuery("DELETE FROM TblTriggerLog");
	}
}
