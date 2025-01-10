using System.Globalization;

namespace Geco.Views.Helpers;

public class HtmlConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is string htmlContent)
		{
			if (htmlContent.Length >= 11 && htmlContent.Trim().StartsWith("`"))
			{
				htmlContent = htmlContent.Substring(7, htmlContent.Length - 11);
			}
			string base64Html = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(htmlContent));
			return $"data:text/html;base64,{base64Html}";
		}
		return null;
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}
