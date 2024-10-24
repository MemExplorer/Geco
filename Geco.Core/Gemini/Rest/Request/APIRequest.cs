using System.Text;
using System.Text.Json;

namespace Geco.Core.Gemini.Rest.Request;

/// <summary>
/// Class responsible for creating web requests
/// </summary>
internal class APIRequest
{
	internal async Task<RequestStatus<ResponseType>> PostAsync<ResponseType>(string uri, string content)
	{
		var strContent = new StringContent(content, Encoding.UTF8, "application/json");
		using var httpClient = new HttpClient();
		var response = await httpClient.PostAsync(uri, strContent);
		return await ValidateResponse<ResponseType>(response);
	}

	internal async Task<RequestStatus<ResponseType>> GetAsync<ResponseType>(string uri)
	{
		using var httpClient = new HttpClient();
		var response = await httpClient.GetAsync(uri);
		return await ValidateResponse<ResponseType>(response);
	}

	private async Task<RequestStatus<ResponseType>> ValidateResponse<ResponseType>(HttpResponseMessage? response)
	{
		if (response == null || !response.IsSuccessStatusCode)
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
}
