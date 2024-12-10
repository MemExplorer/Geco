using System.Reflection;
using System.Text.RegularExpressions;

namespace Geco.Core;
internal partial class StringHelpers
{
	[GeneratedRegex("\\{[A-Za-z_][A-Za-z0-9_]*\\}")]
	private static partial Regex GetNamedPlaceholderPattern();

	public static string FormatString(string template, object dataFields)
	{
		var pattern = GetNamedPlaceholderPattern();
		var typeProperties = dataFields.GetType().GetProperties();
		var typePropertyDict = typeProperties.ToDictionary(f => f.Name, f => f);
		return pattern.Replace(template, m =>
		{
			if (typePropertyDict.TryGetValue(m.Value[1..(m.Value.Length - 1)], out PropertyInfo? propInfo))
				return (string)(propInfo.GetValue(dataFields) ?? string.Empty);

			return m.Value;
		});
	}
}
