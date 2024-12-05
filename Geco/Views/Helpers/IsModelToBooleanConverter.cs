using System.Globalization;
using Microsoft.Extensions.AI;

namespace Geco.Views.Helpers;

public class IsModelToBooleanConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
		value is ChatRole { Value: "model" };

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
		throw new NotImplementedException();
}
