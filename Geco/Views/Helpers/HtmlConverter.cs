using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Geco.Views.Helpers;

public class HtmlConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is string text)
		{
			text = Regex.Replace(text, @"^```html|```$", string.Empty, RegexOptions.Multiline).Trim();

			text = text.Replace("&lt;", "<")
					   .Replace("&gt;", ">")
					   .Replace("&quot;", "\"");
			return text;
		}
		return value ?? string.Empty;
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}
