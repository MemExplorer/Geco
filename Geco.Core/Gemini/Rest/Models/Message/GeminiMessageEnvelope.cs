using System.Text.Json;
using System.Text.Json.Serialization;

namespace Geco.Core.Gemini.Rest.Models.Message;

public readonly record struct GeminiMessageEnvelope(
    [property: JsonPropertyName("contents")] List<MessageContent> Content
)
{
    public static string WrapMessage(List<MessageContent> messages)
    {
        var wrappedMsg = new GeminiMessageEnvelope(messages);
        return JsonSerializer.Serialize(wrappedMsg);
    }
}
