using System.Globalization;
using System.Text;

namespace Geco.Views.Helpers;

public class HtmlConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is string htmlContent)
		{
			htmlContent = htmlContent.Trim();
			if (htmlContent.StartsWith("```html", StringComparison.InvariantCultureIgnoreCase))
				htmlContent = htmlContent[7 .. (htmlContent.Length - 3)].Trim();

			string base64Html = System.Convert.ToBase64String(Encoding.UTF8.GetBytes(htmlContent));
			string urlEncodedHtmlContent = Uri.EscapeDataString(base64Html);
			return $"data:text/html;base64,{urlEncodedHtmlContent}";
		}
		return null;
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}
