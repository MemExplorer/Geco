using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Geco.Core.Brave;

public class SearchAPI
{
	const string APIEndpoint = "https://api.search.brave.com/res/v1";
	private HttpClient Client { get; }

	public SearchAPI(string apiKey)
	{
		Client = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip });
		var mediaType = MediaTypeWithQualityHeaderValue.Parse("application/json");
		Client.DefaultRequestHeaders.Accept.Add(mediaType);
		Client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
		Client.DefaultRequestHeaders.Add("X-Subscription-Token", apiKey);
	}

	public async Task<IList<WebResultEntry>> Search(string query, uint page = 1, uint resultCount = 5)
	{
		uint offset = page - 1;
		string escapedQuery = Uri.EscapeDataString(query + " +sustainable");
		var result =
			await Client.GetAsync(
				$"{APIEndpoint}/web/search?q={escapedQuery}&country=ph&safesearch=strict&result_filter=web&text_decorations=0&offset={offset}&count={resultCount}");
		if (!result.IsSuccessStatusCode)
			throw new WebException(await result.Content.ReadAsStringAsync());

		var content = await result.Content.ReadFromJsonAsync(JsonContext.Default.BraveSearchData);
		if (content == null)
			throw new Exception("Search result is null");

		return content.Web.Results;
	}
}
