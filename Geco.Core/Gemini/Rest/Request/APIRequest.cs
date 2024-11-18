using System.Text;
using System.Text.Json;

namespace Geco.Core.Gemini.Rest.Request;

/// <summary>
///     Class responsible for creating web requests
/// </summary>
class ApiRequest
{
	internal async Task<RequestStatus<TResponseType>> PostAsync<TResponseType>(string uri, string content)
	{
		var strContent = new StringContent(content, Encoding.UTF8, "application/json");
		using var httpClient = new HttpClient();
		var response = await httpClient.PostAsync(uri, strContent);
		return await ValidateResponse<TResponseType>(response);
	}

	internal async Task<RequestStatus<TResponseType>> GetAsync<TResponseType>(string uri)
	{
		using var httpClient = new HttpClient();
		var response = await httpClient.GetAsync(uri);
		return await ValidateResponse<TResponseType>(response);
	}

	async Task<RequestStatus<TResponseType>> ValidateResponse<TResponseType>(HttpResponseMessage? response)
	{
		if (response is not { IsSuccessStatusCode: true })
		{
			return default; // assume new(false, default(ResponseType))
		}

		string strJsonResponse = await response.Content.ReadAsStringAsync();
		var deserializedResponse = JsonSerializer.Deserialize<TResponseType>(strJsonResponse);
		if (deserializedResponse == null)
		{
			return default; // assume new(false, default(ResponseType))
		}

		return new RequestStatus<TResponseType>(true, deserializedResponse);
	}
}
