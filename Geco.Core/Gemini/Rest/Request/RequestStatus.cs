namespace Geco.Core.Gemini.Rest.Request;

/// <summary>
/// Either <typeparamref name="RequestType"/> or <c>null</c>
/// depending on the result of the request
/// </summary>
/// <typeparam name="RequestType"></typeparam>
/// <param name="Success">A value indicating whether the request was successful or not</param>
/// <param name="Content">Gives a <c>null</c> value when the request failed<br></br>
/// else, it will give a <typeparamref name="RequestType"/> when the request is successful</param>
public readonly record struct RequestStatus<RequestType>(
	bool Success,
	RequestType? Content
)
{
}
