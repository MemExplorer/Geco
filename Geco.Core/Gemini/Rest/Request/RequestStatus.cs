namespace Geco.Core.Gemini.Rest.Request;

public readonly record struct RequestStatus<RequestType>(
	bool Success,
	RequestType? Content
)
{
}
