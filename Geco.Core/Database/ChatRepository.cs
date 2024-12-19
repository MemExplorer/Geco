using Geco.Core.Database.SqliteModel;
using Geco.Core.Models.Chat;
using Microsoft.Extensions.AI;

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

	public async Task AppendHistory(GecoConversation history)
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
		var additionalProperties = message.AdditionalProperties;
		if (additionalProperties == null)
			throw new NullReferenceException("Additional properties cannot be null");
		if (!additionalProperties.TryGetValue("id", out ulong? msgId))
			throw new NullReferenceException("MessageId cannot be null");
		if (message.Text == null)
			throw new NullReferenceException("Text cannot be null");
		await db.ExecuteNonQuery("INSERT INTO TblChatMessage VALUES(?, ?, ?, ?)", historyId, msgId,
			message.Text, message.Role.Value);
	}

	public async Task LoadHistory(ICollection<GecoConversation> historyData)
	{
		await Initialize();

		using var db = await SqliteDb.GetTransient(DatabaseDir);
		await using var historyReader = await db.ExecuteReader("SELECT * FROM TblChatHistory ORDER BY DateCreated ASC");
		while (historyReader.Read())
		{
			var historyEntry = new GecoConversation((string)historyReader["Id"], (string)historyReader["Title"],
				(long)historyReader["DateCreated"], []);
			historyData.Add(historyEntry);
		}
	}

	public async Task LoadChats(GecoConversation history)
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
			ulong msgId = (ulong)(long)chatReader["MessageId"];
			string chatContent = (string)chatReader["Content"];
			string chatRole = (string)chatReader["Role"];
			var chatEntry = new ChatMessage
			{
				Text = chatContent,
				Role = new ChatRole(chatRole),
				AdditionalProperties = new AdditionalPropertiesDictionary { ["id"] = msgId }
			};
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
