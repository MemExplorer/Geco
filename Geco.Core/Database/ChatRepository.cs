using Geco.Core.Database.SqliteModel;
using Geco.Core.Gemini;

namespace Geco.Core.Database;

public class ChatRepository : DbRepositoryBase
{
	public ChatRepository(string databaseDir) : base(databaseDir)
	{
	}
	
	// Database table blueprint
	internal override TblSchema[]? TableSchemas =>
	[
		new TblSchema("TblChatHistory", [
			new TblField("Id", TblFieldType.Text, true),
			new TblField("Title", TblFieldType.Integer),
			new TblField("DateCreated", TblFieldType.Integer)
		]),
		new TblSchema("TblChatMessage", [
			new TblField("HistoryId", TblFieldType.Text),
			new TblField("MessageId", TblFieldType.Integer),
			new TblField("Content", TblFieldType.Text),
			new TblField("Role", TblFieldType.Text)
		])
	];

	public async Task AppendHistory(ChatHistory history)
	{
		await Initialize();

		using var db = await SqliteDb.GetTransient(DatabaseDir);
		await db.ExecuteNonQuery("INSERT INTO TblChatHistory VALUES(?, ?, ?)", history.Id, history.Title,
			history.DateCreated);
		foreach (var message in history.Messages)
			await AppendChat(history.Id, message);
	}

	public async Task AppendChat(string historyId, ChatMessage message)
	{
		await Initialize();

		using var db = await SqliteDb.GetTransient(DatabaseDir);
		await db.ExecuteNonQuery("INSERT INTO TblChatMessage VALUES(?, ?, ?, ?)", historyId, message.MessageId,
			message.Text, message.Role ?? "");
	}

	public async Task LoadHistory(ICollection<ChatHistory> historyData)
	{
		await Initialize();

		using var db = await SqliteDb.GetTransient(DatabaseDir);
		await using var historyReader = await db.ExecuteReader("SELECT * FROM TblChatHistory ORDER BY DateCreated ASC");
		while (historyReader.Read())
		{
			var historyEntry = new ChatHistory((string)historyReader["Id"], (string)historyReader["Title"],
				(long)historyReader["DateCreated"], []);
			historyData.Add(historyEntry);
		}
	}

	public async Task LoadChats(ChatHistory history)
	{
		await Initialize();

		// ensure history has no messages
		history.Messages.Clear();

		// fetch messages from database
		using var db = await SqliteDb.GetTransient(DatabaseDir);
		await using var chatReader =
			await db.ExecuteReader("SELECT * FROM TblChatMessage WHERE HistoryId = ? ORDER BY MessageId ASC",
				history.Id);
		while (chatReader.Read())
		{
			var chatEntry = new ChatMessage((ulong)(long)chatReader["MessageId"], (string)chatReader["Content"],
				(string)chatReader["Role"]);
			history.Messages.Add(chatEntry);
		}
	}

	public async Task DeleteHistory(string historyId)
	{
		await Initialize();

		using var db = await SqliteDb.GetTransient(DatabaseDir);
		await db.ExecuteNonQuery("DELETE FROM TblChatHistory WHERE Id = ?", historyId);
		await db.ExecuteNonQuery("DELETE FROM TblChatMessage WHERE HistoryId = ?", historyId);
	}

	public async Task DeleteAllHistory()
	{
		await Initialize();

		using var db = await SqliteDb.GetTransient(DatabaseDir);
		await db.ExecuteNonQuery("DELETE FROM TblChatHistory");
		await db.ExecuteNonQuery("DELETE FROM TblChatMessage");
	}
}
