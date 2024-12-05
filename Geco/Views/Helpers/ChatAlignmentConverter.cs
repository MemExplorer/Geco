using System.Globalization;
using Microsoft.Extensions.AI;

namespace Geco.Views.Helpers;

public class ChatAlignmentConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
		value is ChatRole cr && cr.Value != "model" ? LayoutOptions.End : LayoutOptions.Start;

	// we don't need an implementation for this
	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
		throw new NotImplementedException();
}
