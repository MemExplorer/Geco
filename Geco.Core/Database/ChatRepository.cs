using Geco.Core.Database.SqliteModel;
using Geco.Core.Gemini;

namespace Geco.Core.Database;
public class ChatRepository
{
	private bool _initialized = false;

	// Database table blueprint
	private TblSchema[] TableSchemas { get; } = [
		new("TblChatHistory", [
			new("Id", TblFieldType.TEXT, true),
			new("Title", TblFieldType.INTEGER),
			new("DateCreated", TblFieldType.INTEGER)
		]),
		new("TblChatMessage", [
			new("HistoryId", TblFieldType.TEXT),
			new("MessageId", TblFieldType.INTEGER),
			new("Content", TblFieldType.TEXT),
			new("Role", TblFieldType.TEXT)
		])
	];

	/// <summary>
	/// Creates tables from the blueprint if they don't exist
	/// </summary>
	private async Task InitializeTables()
	{
		// check is only performed once
		if (_initialized)
			return;

		using var db = await SqliteDB.GetTransient();
		foreach (var tblSchema in TableSchemas)
		{
			// check if current table name exists
			var tblExistsQry = await db.ExecuteScalar<long>("SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' and tbl_name = ?", tblSchema.Name);
			if (tblExistsQry != 0)
				continue;

			var tblCreateQry = tblSchema.BuildQuery();
			await db.ExecuteNonQuery(tblCreateQry);
		}

		_initialized = true;
	}

	public async Task AppendHistory(ChatHistory history)
	{
		await InitializeTables();

		using var db = await SqliteDB.GetTransient();
		await db.ExecuteNonQuery("INSERT INTO TblChatHistory VALUES(?, ?, ?)", history.Id, history.Title, history.DateCreated);
		foreach (var message in history.Messages)
			await AppendChat(history.Id, message);
	}

	public async Task AppendChat(string historyId, ChatMessage message)
	{
		await InitializeTables();

		using var db = await SqliteDB.GetTransient();
		await db.ExecuteNonQuery("INSERT INTO TblChatMessage VALUES(?, ?, ?, ?)", historyId, message.MessageId, message.Text, message.Role);
	}

	public async Task LoadHistory(ICollection<ChatHistory> historyData)
	{
		await InitializeTables();

		using var db = await SqliteDB.GetTransient();
		using var historyReader = await db.ExecuteReader("SELECT * FROM TblChatHistory ORDER BY DateCreated ASC");
		while (historyReader.Read())
		{
			var historyEntry = new ChatHistory((string)historyReader["Id"], (string)historyReader["Title"], (long)historyReader["DateCreated"], []);
			historyData.Add(historyEntry);
		}
	}

	public async Task LoadChats(ChatHistory history)
	{
		await InitializeTables();

		// ensure history has no messages
		history.Messages.Clear();

		// fetch messages from database
		using var db = await SqliteDB.GetTransient();
		using var chatReader = await db.ExecuteReader("SELECT * FROM TblChatMessage WHERE HistoryId = ? ORDER BY MessageId ASC", history.Id);
		while (chatReader.Read())
		{
			var chatEntry = new ChatMessage((ulong)(long)chatReader["MessageId"], (string)chatReader["Content"], (string)chatReader["Role"]);
			history.Messages.Add(chatEntry);
		}
	}

	public async Task DeleteHistory(string historyId)
	{
		await InitializeTables();

		using var db = await SqliteDB.GetTransient();
		await db.ExecuteNonQuery("DELETE FROM TblChatHistory WHERE Id = ?", historyId);
		await db.ExecuteNonQuery("DELETE FROM TblChatMessage WHERE HistoryId = ?", historyId);
	}
}
