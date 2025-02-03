namespace Geco;

internal static class GlobalContext
{
	static string? chatHtmlTemplate = null;
	internal static IServiceProvider Services
	{
		get
		{
			var app = IPlatformApplication.Current;
			if (app == null)
				throw new InvalidOperationException(
					"Cannot resolve current application. Services should be accessed after MauiProgram initialization.");
			return app.Services;
		}
	}

	internal static string ChatHtmlTemplate
	{
		get
		{
			if (chatHtmlTemplate != null) 
				return chatHtmlTemplate;
			
			using var stream = FileSystem.OpenAppPackageFileAsync("ChatTemplate.html");
			using var reader = new StreamReader(stream.Result);
			chatHtmlTemplate = reader.ReadToEnd();
			return chatHtmlTemplate;
		}
	}

	internal static DebugLogger Logger => Services.GetRequiredService<DebugLogger>();

	// Gemini Constants
	internal const int GeminiChat = 0;
	internal const int GeminiNotification = 1;
	internal const int GeminiWeeklyReport = 2;
	internal const int GeminiSearchSummary = 3;
}
