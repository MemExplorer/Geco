using Geco.Core.Database.SqliteModel;

namespace Geco.Core.Database;

public class TriggerRepository : DbRepositoryBase
{
	public TriggerRepository(string databaseDir) : base(databaseDir)
	{
	}

	// Database table blueprint
	internal override TblSchema[]? TableSchemas =>
	[
		new TblSchema("TblTriggerLog", [
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

	public async Task<IList<TriggerInfo>> FetchTriggerRecords()
	{
		await Initialize();

		using var db = await SqliteDb.GetTransient(DatabaseDir);

		// ensure that the fetched data corresponds to records from the last 7 days.
		var triggers = new List<TriggerInfo>();
		await using var fetchQuery = await db.ExecuteReader(
			"SELECT Type, SUM(RawValue) FROM TblTriggerLog WHERE (unixepoch() - Timestamp) <= 604800 GROUP BY Type");
		while (fetchQuery.Read())
			triggers.Add(new TriggerInfo((DeviceInteractionTrigger)(long)fetchQuery["Type"],
				(int)(long)fetchQuery["SUM(RawValue)"]));

		return triggers;
	}

	public async Task PurgeWeeklyTriggerData()
	{
		await Initialize();

		using var db = await SqliteDb.GetTransient(DatabaseDir);

		// only delete data that's been in the database for more than 7 days
		await db.ExecuteNonQuery("DELETE FROM TblTriggerLog WHERE (unixepoch() - Timestamp) > 604800");
	}

	public async Task PurgeTriggerData()
	{
		await Initialize();

		using var db = await SqliteDb.GetTransient(DatabaseDir);
		await db.ExecuteNonQuery("DELETE FROM TblTriggerLog");
	}
}
