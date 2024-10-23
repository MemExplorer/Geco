using Geco.Core.Gemini;
using System.Collections.ObjectModel;

namespace Geco.Models.Chat;

public readonly record struct ChatHistory(string Id, string Title, ObservableCollection<ChatMessage> Messages)
{
}
