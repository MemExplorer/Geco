using System.Diagnostics;
using System.Text.Json;

namespace Geco.Core.Gemini.Rest;

internal class Request : IDisposable
{
    private HttpClient _httpClient { get; }

    internal Request()
    {
        _httpClient = new HttpClient();
    }

    internal async Task<RequestStatus<ResponseType>> PostAsync<ResponseType>(string uri, string content)
    {
        StringContent strContent = new StringContent(content);
        var response = await _httpClient.PostAsync(uri, strContent);
        if (response == null)
        {
            return default; // assume new(false, default(ResponseType))
        }

        var strJsonResponse = await response.Content.ReadAsStringAsync();
        var deserializedResponse = JsonSerializer.Deserialize<ResponseType>(strJsonResponse);
        if (deserializedResponse == null)
        {
            return default; // assume new(false, default(ResponseType))
        }

        return new(true, deserializedResponse);
    }

    internal async Task<RequestStatus<ResponseType>> GetAsync<ResponseType>(string uri)
    {
        var response = await _httpClient.GetAsync(uri);
        if (response == null)
        {
            return default; // assume new(false, default(ResponseType))
        }

        var strJsonResponse = await response.Content.ReadAsStringAsync();
        var deserializedResponse = JsonSerializer.Deserialize<ResponseType>(strJsonResponse);
        if (deserializedResponse == null)
        {
            return default; // assume new(false, default(ResponseType))
        }

        return new(true, deserializedResponse);
    }

    public void Dispose()
    {
        if (_httpClient != null)
            _httpClient.Dispose();
    }
}
