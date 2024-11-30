using Geco.Core.Database.SqliteModel;

namespace Geco.Core.Database;

public abstract class DbRepositoryBase
{
	bool _initialized;
	protected string DatabaseDir { get; }
	protected DbRepositoryBase(string databaseDir) =>
		DatabaseDir = databaseDir;

	// Database table blueprint
	internal abstract TblSchema[]? TableSchemas { get; }

	/// <summary>
	///     Creates tables from the blueprint if they don't exist
	/// </summary>
	protected virtual async Task InitializeTables()
	{
		using var db = await SqliteDb.GetTransient(DatabaseDir);
		foreach (var tblSchema in TableSchemas!)
		{
			// check if current table name exists
			long tblExistsQry =
				await db.ExecuteScalar<long>("SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' and tbl_name = ?",
					tblSchema.Name);
			if (tblExistsQry != 0)
				continue;

			string tblCreateQry = tblSchema.BuildQuery();
			await db.ExecuteNonQuery(tblCreateQry);
		}
	}

	protected async Task Initialize()
	{
		// check is only performed once
		if (_initialized)
			return;

		await InitializeTables();

		_initialized = true;
	}
}
