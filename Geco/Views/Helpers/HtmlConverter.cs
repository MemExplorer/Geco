using System.Globalization;
using Geco.Core;
using Markdig;

namespace Geco.Views.Helpers;

public class HtmlConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		const string WeeklyReportHeader = "weeklyreport";
		if (value is not string markdownContent)
			return null;

		string backgroundColor = GecoSettings.DarkMode ? "#1f1f1f" : "#FFFFFF";
		string textColor = GecoSettings.DarkMode ? "#ffffff" : "#000000";

		// override content if when we detect the weekly report header
		if (markdownContent.StartsWith(WeeklyReportHeader))
			markdownContent = StringHelpers.FormatString(markdownContent[WeeklyReportHeader.Length..],
				new { BgColor = backgroundColor, FgColor = textColor });
		else
		{
			markdownContent = markdownContent.Trim();
			var pipeline = GlobalContext.Services.GetRequiredService<MarkdownPipeline>();
			markdownContent = Markdown.ToHtml(markdownContent, pipeline);
			markdownContent = StringHelpers.FormatString(GlobalContext.ChatHtmlTemplate,
				new { MdContent = markdownContent, BgColor = backgroundColor, FgColor = textColor });
		}

		return markdownContent;
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
		throw new NotImplementedException();
}
