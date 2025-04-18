using CommunityToolkit.Maui;
using Geco.Core.Brave;
using Geco.Core.Database;
using Geco.Core.Models.ActionObserver;
using Geco.Core.Models.Notification;
using Geco.ViewModels;
using Geco.Views;
using Geco.Views.Helpers;
using GoogleGeminiSDK;
using GoogleGeminiSDK.Models.Components;
using GoogleGeminiSDK.Models.ContentGeneration;
using Markdig;
using MPowerKit.VirtualizeListView;
using Syncfusion.Maui.Toolkit.Hosting;
#if ANDROID
using Application = Android.App.Application;
using Geco.Triggers.ActionObservers;
using Geco.Notifications;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using Geco.Platforms.Android.Handlers;
using Android.Content.Res;
#endif


namespace Geco;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.ConfigureSyncfusionToolkit()
			.UseMPowerKitListView()
			.UseContentSizedWebview()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
				fonts.AddFont("Poppins-Regular.ttf", "Poppins");
				fonts.AddFont("FontAwesome6DuotoneNew-Solid.ttf", "FontAwesome");
			})
			.InitializeUiServices()
			.InitializeDatabaseServices()
			.InitializeLoggerService()
			.InitializeAndroidServices()
			.InitializeBackendServices();

		// platform specific modifications
		ApplyAndroidUiModifications();

		return builder.Build();
	}

	// Reference: https://github.com/albyrock87/maui-content-sized-webview
	static MauiAppBuilder UseContentSizedWebview(this MauiAppBuilder builder)
	{
		builder.ConfigureMauiHandlers(handlers =>
		{
#if ANDROID
			handlers.AddHandler<ContentSizedWebView, ContentSizedWebViewHandler>();
#endif
		});

		return builder;
	}

	static MauiAppBuilder InitializeBackendServices(this MauiAppBuilder builder)
	{
		builder.InitializeGeminiServices();
		builder.Services.AddSingleton(new SearchAPI(GecoSecrets.BRAVE_SEARCH_API_KEY));
		builder.Services.AddSingleton(new MarkdownPipelineBuilder().UseAdvancedExtensions().Build());
		return builder;
	}

	static MauiAppBuilder InitializeLoggerService(this MauiAppBuilder builder)
	{
#if ANDROID
		string dataDir = Application.Context.GetExternalFilesDir(null)!.AbsoluteFile.Path;
#else
		string dataDir = FileSystem.AppDataDirectory;
#endif
		string filePath = Path.Combine(dataDir, "log.txt");
		builder.Services.AddSingleton<DebugLogger>(_ => new DebugLogger(filePath));
		return builder;
	}

	static MauiAppBuilder InitializeGeminiServices(this MauiAppBuilder builder)
	{
		builder.Services.AddKeyedSingleton(GlobalContext.GeminiChat, new GeminiSettings
		{
			Temperature = 1,
			TopP = 1,
			TopK = 40,
			SafetySettings = new List<SafetySetting>
			{
				new(HarmCategory.HARM_CATEGORY_SEXUALLY_EXPLICIT, HarmBlockThreshold.BLOCK_LOW_AND_ABOVE),
				new(HarmCategory.HARM_CATEGORY_DANGEROUS_CONTENT, HarmBlockThreshold.BLOCK_LOW_AND_ABOVE),
				new(HarmCategory.HARM_CATEGORY_HARASSMENT, HarmBlockThreshold.BLOCK_LOW_AND_ABOVE),
				new(HarmCategory.HARM_CATEGORY_HATE_SPEECH, HarmBlockThreshold.BLOCK_LOW_AND_ABOVE),
				new(HarmCategory.HARM_CATEGORY_CIVIC_INTEGRITY, HarmBlockThreshold.BLOCK_LOW_AND_ABOVE)
			},
			SystemInstructions =
				"""
				## Overview  
				You are **GECO (Green Efforts on Carbon)**, a large language model based on Google Gemini, currently integrated into a mobile application. Developed by **SS Bois**, your primary purpose is to promote sustainability by encouraging eco-friendly habits and practices.

				## Purpose and Features  
				As GECO, you serve as a **personalized sustainability assistant** with two main functionalities:  
				1. **Sustainable Chat Bot**:  
				    - Provides advice and resources in a **conversation-like tone**.  
				    - Focused on creating an engaging and interactive experience.  
				2. **Sustainable Search Engine**:  
				    - Delivers information and recommendations in a **search engine-like manner**.  
				    - Offers concise and resourceful responses tailored to sustainability.  

				## Mobile Device Observations  
				You are capable of monitoring specific aspects of a user’s mobile device usage (if enabled in settings). These include:  
				- Battery charging habits  
				- Screen time  
				- Use of location services  
				- Use of network services  
				- GECO Search activity  

				If **Mobile Habit Monitoring** is enabled, GECO will generate a **weekly sustainability likelihood report** notification based on the user’s habits, assessing how they align with sustainable practices.

				## Response Format  
				- All responses must be presented in **Markdown**.
				- Ensure guidance is **clear, straightforward, and formatted appropriately**.  
				- When asking for directions, provide a visually appealing (Emoji or Unicode characters) step-by-step tutorial.

				## App Layout and Navigation  
				- The app’s starting page is the **Sustainable Chat** page.  
				- On the **upper left**, a **navigation menu** can be toggled, revealing the following options (in order):  
				  1. Chat  
				  2. Search  
				  3. Weekly Reports
				  4. Conversation History  
				  5. Settings (at the bottom right of the flyout menu)  

				- **Settings Page Options**:  
				  - Clear all conversations  
				  - Switch between **light** and **dark mode**  
				  - Enable/disable **Mobile Habit Monitoring**  
				  - Enable/disable notifications  

				Don't forget to mention that you can also navigate to the Search, Weekly Reports, and Settings pages when the user commands it by asking you through the Chat Page's chat box.
				
				## Current Context  
				You are currently operating in the **Sustainable Chat** page, providing users with tailored sustainability advice in a conversational tone.
				"""
		});

		builder.Services.AddKeyedSingleton(GlobalContext.GeminiNotification, new GeminiSettings
		{
			SystemInstructions =
				"You are Geco, a large language model based on Google Gemini. You are developed by SS Bois. Your response should always be sustainability focused. The contents of the 'FullContent' property must be presented in **Markdown**.",
			Conversational = false,
			ResponseMimeType = "application/json",
			ResponseSchema = new Schema(
				SchemaType.ARRAY,
				Items: new Schema(SchemaType.OBJECT,
					Properties: new Dictionary<string, Schema>
					{
						{ "NotificationTitle", new Schema(SchemaType.STRING) },
						{ "NotificationDescription", new Schema(SchemaType.STRING) },
						{ "FullContent", new Schema(SchemaType.STRING) }
					},
					Required: ["NotificationTitle", "NotificationDescription", "FullContent"]
				)
			)
		});

		builder.Services.AddKeyedSingleton(GlobalContext.GeminiWeeklyReport, new GeminiSettings
		{
			Conversational = false,
			ResponseMimeType = "application/json",
			ResponseSchema = new Schema(
				SchemaType.ARRAY,
				Items: new Schema(SchemaType.OBJECT,
					Properties: new Dictionary<string, Schema>
					{
						{ "NotificationTitle", new Schema(SchemaType.STRING) },
						{ "NotificationDescription", new Schema(SchemaType.STRING) },
						{ "Overview", new Schema(SchemaType.STRING) },
						{ "ReportBreakdown", new Schema(SchemaType.STRING) },
						{ "ComputeBreakdown", new Schema(SchemaType.STRING) }
					},
					Required:
					[
						"NotificationTitle", "NotificationDescription", "Overview", "ReportBreakdown",
						"ComputeBreakdown"
					]
				)
			)
		});

		builder.Services.AddKeyedSingleton(GlobalContext.GeminiSearchSummary, new GeminiSettings
		{
			Conversational = false,
		});

		builder.Services.AddTransient<GeminiChat>(_ =>
			new GeminiChat(GecoSecrets.GEMINI_API_KEY, "gemini-2.0-flash-001"));
		return builder;
	}

	static MauiAppBuilder InitializeUiServices(this MauiAppBuilder builder)
	{
		// page and view model instances
		builder.Services.AddSingleton<AppShellViewModel>();
		builder.Services.AddTransient<ChatPage>();
		builder.Services.AddTransient<ChatViewModel>();
		builder.Services.AddSingleton<SearchPage>();
		builder.Services.AddSingleton<SearchViewModel>();
		builder.Services.AddSingleton<SearchResultPage>();
		builder.Services.AddSingleton<SearchResultViewModel>();
		builder.Services.AddTransient<ReportsPage>();
		builder.Services.AddTransient<ReportsViewModel>();
		builder.Services.AddTransient<WeeklyReportChatPage>();
		builder.Services.AddTransient<WeeklyReportChatViewModel>();
		builder.Services.AddSingleton<StartupPage>();
		builder.Services.AddSingleton<StartupPageViewModel>();
		return builder;
	}

	static MauiAppBuilder InitializeDatabaseServices(this MauiAppBuilder builder)
	{
		// data repository instances
#if ANDROID
		string dataDir = Application.Context.GetExternalFilesDir(null)!.AbsoluteFile.Path;
#else
		string dataDir = FileSystem.AppDataDirectory;
#endif
		builder.Services.AddSingleton(new ChatRepository(dataDir));
		builder.Services.AddSingleton(new TriggerRepository(dataDir));
		builder.Services.AddSingleton(new PromptRepository(dataDir));
		return builder;
	}

	static MauiAppBuilder InitializeAndroidServices(this MauiAppBuilder builder)
	{
#if ANDROID
		builder.Services.AddTransient<INotificationManagerService, NotificationManagerService>();

		// monitor service
		builder.Services.AddSingleton<IPlatformActionObserver, DeviceUsageMonitorService>();

		// android triggers
		builder.Services.AddSingleton<IDeviceStateObserver, NetworkStateObserver>();
		builder.Services.AddSingleton<IDeviceStateObserver, LocationStateObserver>();
		builder.Services.AddSingleton<IDeviceStateObserver, BatteryStateObserver>();
#endif
		return builder;
	}

	static void ApplyAndroidUiModifications()
	{
#if ANDROID
		// Adjust header title position
		ToolbarHandler.Mapper.AppendToMapping("CustomNavigationView", (handler, _) =>
		{
			handler.PlatformView.ContentInsetStartWithNavigation = 0;
		});

		// Remove underscore in Entry Control
		EntryHandler.Mapper.AppendToMapping("NoUnderline", (h, _) =>
		{
			h.PlatformView.BackgroundTintList =
				ColorStateList.ValueOf(Colors.Transparent.ToPlatform());
		});

		// Remove underscore in Editor Control
		EditorHandler.Mapper.AppendToMapping("NoUnderline", (h, _) =>
		{
			h.PlatformView.BackgroundTintList =
				ColorStateList.ValueOf(Colors.Transparent.ToPlatform());
		});

		// Hide scrollbar in webview
		WebViewHandler.Mapper.AppendToMapping("RemoveScrollbars", (handler, view) =>
		{
			var nativeWebView = handler.PlatformView;
			nativeWebView.VerticalScrollBarEnabled = false;
			nativeWebView.HorizontalScrollBarEnabled = false;
		});
#endif
	}
}
