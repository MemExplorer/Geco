﻿using System.Globalization;
using Microsoft.Extensions.AI;
using Microsoft.Maui.Controls.Shapes;

namespace Geco.Views.Helpers;

public class IsModelToShapeConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		var isModel = value is ChatRole { Value: "model" };
		var modelBubbleShape = new CornerRadius(34, 34, 8, 34);
		var userBubbleShape = new CornerRadius(34, 34, 34, 8);
		return new RoundRectangle()
		{
			CornerRadius = isModel ? modelBubbleShape : userBubbleShape
		};
	}
		

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
		throw new NotImplementedException();
}
