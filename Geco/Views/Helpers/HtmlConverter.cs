using System.Globalization;
using System.Text;
using Geco.Core;

namespace Geco.Views.Helpers;

public class HtmlConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		const string WeeklyReportHeader = "weeklyreport";
		if (value is not string markdownContent)
			return null;

		// override content if when we detect the weekly report header
		if (markdownContent.StartsWith(WeeklyReportHeader))
			markdownContent = markdownContent[WeeklyReportHeader.Length..];
		else
		{
			using var stream = FileSystem.OpenAppPackageFileAsync("ChatTemplate.html");
			using var reader = new StreamReader(stream.Result);
			string? htmlTemplate = reader.ReadToEnd();

			markdownContent = markdownContent.Trim();
			markdownContent = StringHelpers.FormatString(htmlTemplate, new { MdContent = markdownContent });
		}

		string base64Html = System.Convert.ToBase64String(Encoding.UTF8.GetBytes(markdownContent));
		string urlEncodedHtmlContent = Uri.EscapeDataString(base64Html);
		return $"data:text/html;base64,{urlEncodedHtmlContent}";
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
		throw new NotImplementedException();
}
