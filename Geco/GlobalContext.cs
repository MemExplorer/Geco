namespace Geco;

internal static class GlobalContext
{
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

	internal static DebugLogger Logger => Services.GetRequiredService<DebugLogger>();

	// Gemini Constants
	internal const int GeminiChat = 0;
	internal const int GeminiNotification = 1;
	internal const int GeminiWeeklyReport = 2;
}
