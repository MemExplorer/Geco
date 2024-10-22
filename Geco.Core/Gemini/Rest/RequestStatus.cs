
namespace Geco.Core.Gemini.Rest
{
    public readonly record struct RequestStatus<RequestType>(bool Success, RequestType? Content)
    {
    }
}
