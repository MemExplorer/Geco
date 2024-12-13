
namespace Geco.Views;

public partial class SearchResultPage : ContentPage, IQueryAttributable
{
	public string? Query { get; set; }

	public SearchResultPage()
	{
		InitializeComponent();
	}

	public void ApplyQueryAttributes(IDictionary<string, object> query)
	{
		if (query.ContainsKey("Query"))
		{
			Query = (string)query["Query"];

			SearchEntry.Text = $"{Query}";
		}
	}
}
