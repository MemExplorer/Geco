using System.Globalization;

namespace Geco.Views.Helpers;

public class ChatAlignmentConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
		value is bool and false ? LayoutOptions.End : LayoutOptions.Start;

	// we don't need an implementation for this
	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
		throw new NotImplementedException();
}
