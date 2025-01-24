namespace Geco.Core.Brave;

public record BraveSearchData(BraveSearchResult Web);

public record BraveSearchResult(IList<WebResultEntry> Results);

public record WebResultEntry(
	string Title,
	Uri Url,
	string Description,
	DateTime PageAge,
	WebPageUrlMeta MetaUrl,
	IList<string>? ExtraSnippets);

public record WebPageUrlMeta(string Scheme, string Netloc, string Hostname, Uri Favicon, string Path);
