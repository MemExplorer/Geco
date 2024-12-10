using System.Text.RegularExpressions;

namespace Geco.Core;
internal partial class StringHelpers
{
	[GeneratedRegex("\\{[A-Za-z_][A-Za-z0-9_]*\\}")]
	private static partial Regex GetNamedPlaceholderPattern();

	public static string FormatString(string template, object fields)
	{
		var pattern = GetNamedPlaceholderPattern();
		var typeProperties = fields.GetType().GetProperties();
		var typePropertyNames = typeProperties.Select(f => f.Name).ToList();
		var typeValues = typeProperties.Select(f => f.GetValue(fields)).ToList();
		for (int i = 0; i < typePropertyNames.Count; i++)
			template = pattern.Replace(template, m =>
			{
				if (m.Value == $"{{{typePropertyNames[i]}}}")
					return (string)(typeValues[i] ?? "");

				return m.Value;
			});

		return template;
	}
}
