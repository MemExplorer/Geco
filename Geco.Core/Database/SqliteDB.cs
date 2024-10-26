
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

namespace Geco.Core.Database;
public partial class SqliteDB : IDisposable
{
	const string FILENAME = "geco.db";
	protected SqliteConnection? Connection { get; private set; }

	private SqliteDB() { }

	// replace placeholders with id to replace them values according to their position in the `sqlArgs`
	private SqliteCommand PrepareCommand(string query, params object[] sqlArgs)
	{
		int replaceCounter = 0;
		string preprocessedCmd = GetPlaceHolderPattern().Replace(query, m => "@" + replaceCounter++);
		var sqlCmd = new SqliteCommand(preprocessedCmd, Connection);
		for (int i = 0; i < sqlArgs.Length; i++)
			sqlCmd.Parameters.AddWithValue("@" + i, sqlArgs[i]);

		return sqlCmd;
	}

	public async Task<int> ExecuteNonQuery(string query, params object[] sqlArgs)
	{
		using var _command = PrepareCommand(query, sqlArgs);
		return await _command.ExecuteNonQueryAsync();
	}

	public async Task<SqliteDataReader> ExecuteReader(string query, params object[] sqlArgs)
	{
		var _command = PrepareCommand(query, sqlArgs);
		return await _command.ExecuteReaderAsync();
	}

	// Explicitly use the appropriate data type
	public async Task<SqlDataType?> ExecuteScalar<SqlDataType>(string query, params object[] sqlArgs)
	{
		using var _command = PrepareCommand(query, sqlArgs);
		return (SqlDataType?)await _command.ExecuteScalarAsync();
	}

	public void Dispose() => Connection!.Dispose();

	// Regex pattern that captures all single question mark that is not inside a double single-quote statement
	[GeneratedRegex("(?<!\\?)\\?(?!\\?)(?=(?:[^']*'[^']*')*[^']*$)")]
	private static partial Regex GetPlaceHolderPattern();

	public static async Task<SqliteDB> GetTransient()
	{
		// initialize database connection
		var dbPath = Path.Combine(FileSystem.AppDataDirectory, FILENAME);
		var instance = new SqliteDB
		{
			Connection = new SqliteConnection($"Data Source={dbPath}")
		};

		// create db connection
		await instance.Connection.OpenAsync();
		if (instance.Connection.State != System.Data.ConnectionState.Open)
			throw new Exception("Database connection Error!");

		return instance;
	}
}
