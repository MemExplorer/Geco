using System.Collections.ObjectModel;
using Geco.Core.Gemini;

namespace Geco.Models.Chat;

/// <summary>
/// Gemini chat history model
/// </summary>
/// <param name="Id">Unique identifier for chat history (GUID)</param>
/// <param name="Title">Chat title</param>
/// <param name="Messages">All the message in the conversation</param>
public readonly record struct ChatHistory(string Id, string Title, ObservableCollection<ChatMessage> Messages)
{
}
