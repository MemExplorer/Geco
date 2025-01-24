using System.Text.Json.Serialization;

namespace Geco.Core.Brave;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
	DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, UseStringEnumConverter = true)]
[JsonSerializable(typeof(BraveSearchData))]
[JsonSerializable(typeof(BraveSearchResult))]
[JsonSerializable(typeof(WebResultEntry))]
[JsonSerializable(typeof(WebPageUrlMeta))]
internal sealed partial class JsonContext : JsonSerializerContext;
