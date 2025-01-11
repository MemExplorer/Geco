using System.Text;
using Microsoft.Extensions.Primitives;

namespace Geco;

internal class DebugLogger
{
	string LogFilePath { get; }
	internal DebugLogger(string writePath) => 
		LogFilePath = writePath;

	internal void Info<TCurrentClass>(string message) =>
		WriteToFile($"{typeof(TCurrentClass)} - {message}");
	
	internal void Info<TCurrentClass>(ReadOnlySpan<char> message) =>
		WriteToFile($"{typeof(TCurrentClass)} - {message}");

	internal void Error<TCurrentClass>(Exception message, string? additionalDetails = null)
	{
		var sb = new StringBuilder();
		sb.Append(typeof(TCurrentClass));
		sb.Append(" - ");
		if (additionalDetails is not null)
		{
			sb.AppendLine(additionalDetails);
			sb.Append("Stack Trace: ");
		}
		
		sb.Append(message);
		WriteToFile(sb.ToString());
	}
		

	private void WriteToFile(string message)
	{
		var fMessage = BuildMessage(message);
		using var writer = new StreamWriter(LogFilePath, true);
		writer.WriteLine(fMessage);
	}

	private string BuildMessage(string message)
	{
		var sb = new StringBuilder();
		sb.Append('[');
		sb.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
		sb.Append("]: ");
		sb.Append(message);
		return sb.ToString();
	}
}
