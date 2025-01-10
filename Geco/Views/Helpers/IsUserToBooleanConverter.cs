using System.Globalization;
using Microsoft.Extensions.AI;

namespace Geco.Views.Helpers;

public class IsUserToBooleanConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
		value is ChatRole { Value: "user" };

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
		throw new NotImplementedException();
}
