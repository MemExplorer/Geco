using System.Text.Json.Serialization;

namespace Geco.Core.Gemini.Rest.Models.Message;

public readonly record struct MessagePart(
	[property: JsonPropertyName("text")] string Text
)
{
}
