using System.Globalization;

namespace Geco.Views.Helpers;

public class UnixTimeStampToStringConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is long v)
			return DateTimeOffset.FromUnixTimeSeconds(v).ToString("MMM dd, yyyy");

		return "";
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
		throw new NotImplementedException();
}
