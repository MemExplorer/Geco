using System.Collections.ObjectModel;
using Geco.Core.Gemini;

namespace Geco.Models.Chat;

public readonly record struct ChatHistory(string Id, string Title, ObservableCollection<ChatMessage> Messages)
{
}
