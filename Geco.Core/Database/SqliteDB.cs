using System.Data;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

namespace Geco.Core.Database;

public partial class SqliteDb : IDisposable
{
	const string Filename = "geco.db";

	SqliteDb() { }
	SqliteConnection? Connection { get; init; }

	public void Dispose() => Connection!.Dispose();

	// replace placeholders with id to replace them values according to their position in the `sqlArgs`
	SqliteCommand PrepareCommand(string query, params object[] sqlArgs)
	{
		int replaceCounter = 0;
		string preprocessedCmd = GetPlaceHolderPattern().Replace(query, _ => "@" + replaceCounter++);
		var sqlCmd = new SqliteCommand(preprocessedCmd, Connection);
		for (int i = 0; i < sqlArgs.Length; i++)
			sqlCmd.Parameters.AddWithValue("@" + i, sqlArgs[i]);

		return sqlCmd;
	}

	public async Task<int> ExecuteNonQuery(string query, params object[] sqlArgs)
	{
		await using var command = PrepareCommand(query, sqlArgs);
		return await command.ExecuteNonQueryAsync();
	}

	public async Task<SqliteDataReader> ExecuteReader(string query, params object[] sqlArgs)
	{
		var command = PrepareCommand(query, sqlArgs);
		return await command.ExecuteReaderAsync();
	}

	// Explicitly use the appropriate data type
	public async Task<TSqlDataType?> ExecuteScalar<TSqlDataType>(string query, params object[] sqlArgs)
	{
		await using var command = PrepareCommand(query, sqlArgs);
		return (TSqlDataType?)await command.ExecuteScalarAsync();
	}

	// Regex pattern that captures all single question mark that is not inside a double single-quote statement
	[GeneratedRegex("(?<!\\?)\\?(?!\\?)(?=(?:[^']*'[^']*')*[^']*$)")]
	private static partial Regex GetPlaceHolderPattern();

	public static async Task<SqliteDb> GetTransient(string databaseDir)
	{
		// initialize database connection
		string dbPath = Path.Combine(databaseDir, Filename);
		var instance = new SqliteDb { Connection = new SqliteConnection($"Data Source={dbPath}") };

		// create db connection
		await instance.Connection.OpenAsync();
		if (instance.Connection.State != ConnectionState.Open)
			throw new Exception("Database connection Error!");

		return instance;
	}
}
