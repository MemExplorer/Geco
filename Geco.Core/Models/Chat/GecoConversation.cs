using System.Collections.ObjectModel;
using Microsoft.Extensions.AI;

namespace Geco.Core.Models.Chat;

/// <summary>
///     Chat history model
/// </summary>
/// <param name="Id">Unique identifier for chat history (GUID)</param>
/// <param name="Type">Type of Chat Conversation</param>
/// <param name="Title">Chat title</param>
/// <param name="DateCreated">Creation date of chat in unix timestamp</param>
/// <param name="Messages">All the message in the conversation</param>
public record GecoConversation(
	string Id,
	HistoryType Type,
	string Title,
	long DateCreated,
	ObservableCollection<ChatMessage> Messages,
	string? Description = null,
	string? FullContent = null);
