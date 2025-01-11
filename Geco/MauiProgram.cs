using CommunityToolkit.Maui;
#if ANDROID
using Geco.Triggers.ActionObservers;
using Geco.Notifications;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using Android.Content.Res;
#endif
using Geco.Core.Database;
using Geco.Core.Models.ActionObserver;
using Geco.Core.Models.Notification;
using Geco.ViewModels;
using Geco.Views;
using Microsoft.Extensions.Logging;
using Syncfusion.Maui.Toolkit.Hosting;
using GoogleGeminiSDK;
using GoogleGeminiSDK.Models.ContentGeneration;
using GoogleGeminiSDK.Models.Components;


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
			.InitializeGeminiServices();
		
		// platform specific modifications
		ApplyAndroidUiModifications();

		return builder.Build();
	}

	static MauiAppBuilder InitializeLoggerService(this MauiAppBuilder builder)
	{
#if ANDROID
		string dataDir = Android.App.Application.Context.GetExternalFilesDir(null)!.AbsoluteFile.Path;
#else
		string dataDir = FileSystem.AppDataDirectory;
#endif
		string filePath = Path.Combine(dataDir, "log.txt");
		builder.Services.AddSingleton<DebugLogger>(_ => new DebugLogger(filePath));
		return builder;
	}

	static MauiAppBuilder InitializeGeminiServices(this MauiAppBuilder builder)
	{
		builder.Services.AddKeyedSingleton<GeminiSettings>(GlobalContext.GeminiChat, new GeminiSettings
		{
			Temperature = 0.2f,
			TopP = 0.85f,
			TopK = 50,
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
			- All responses must be presented in **Markdown style** but delivered as **HTML format**.
			- Ensure guidance is **clear, straightforward, and formatted appropriately**.  
			- Text color must only be **black**.  
			- Avoid using font sizes larger than `<h2>`.
			- When asking for directions, provide a visually appealing (Emoji or Unicode characters) step-by-step tutorial.
			
			## App Layout and Navigation  
			- The app’s starting page is the **Sustainable Chat** page.  
			- On the **upper left**, a **navigation menu** can be toggled, revealing the following options (in order):  
			  1. Chat  
			  2. Search  
			  3. Conversation History  
			  4. Settings (at the bottom right of the flyout menu)  
			
			- **Settings Page Options**:  
			  - Clear all conversations  
			  - Switch between **light** and **dark mode**  
			  - Enable/disable **Mobile Habit Monitoring**  
			  - Enable/disable notifications  
			
			## Current Context  
			You are currently operating in the **Sustainable Chat** page, providing users with tailored sustainability advice in a conversational tone.
			"""
		});

		builder.Services.AddKeyedSingleton<GeminiSettings>(GlobalContext.GeminiSearch, new GeminiSettings
		{
			SystemInstructions =
			"You are Geco, a large language model based on Google Gemini. You are developed by SS Bois. Your response should always be sustainability focused, your tone should be like a search engine, and you should always have 3 responses",
			Conversational = false,
			ResponseMimeType = "application/json",
			ResponseSchema = new Schema(
			SchemaType.ARRAY,
			Items: new Schema(SchemaType.OBJECT,
				Properties: new Dictionary<string, Schema>
				{
					{ "Title", new Schema(SchemaType.STRING) }, { "Description", new Schema(SchemaType.STRING) }
				},
				Required: ["Title", "Description"]
			)
		)
		});

		builder.Services.AddKeyedSingleton<GeminiSettings>(GlobalContext.GeminiNotification, new GeminiSettings
		{
			SystemInstructions =
			"You are Geco, a large language model based on Google Gemini. You are developed by SS Bois. Your response should always be sustainability focused. The contents of the 'FullContent' property must be presented in **Markdown style** but delivered as **HTML format**. The biggest font size must not exceed `<h2>`.",
			Conversational = false,
			ResponseMimeType = "application/json",
			ResponseSchema = new Schema(
			SchemaType.ARRAY,
			Items: new Schema(SchemaType.OBJECT,
					Properties: new Dictionary<string, Schema>
					{
						{ "NotificationTitle", new Schema(SchemaType.STRING) },
						{ "NotificationDescription", new Schema(SchemaType.STRING) },
						{ "FullContent", new Schema(SchemaType.STRING)}
					},
					Required: ["NotificationTitle", "NotificationDescription", "FullContent"]
			)
		)
		});

		builder.Services.AddTransient<GeminiChat>(_ => new GeminiChat(GecoSecrets.GEMINI_API_KEY, "gemini-1.5-flash-latest"));
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
		builder.Services.AddTransient<SearchResultPage>();
		builder.Services.AddTransient<SearchResultViewModel>();
		return builder;
	}

	static MauiAppBuilder InitializeDatabaseServices(this MauiAppBuilder builder)
	{
		// data repository instances
#if ANDROID
		string dataDir = Android.App.Application.Context.GetExternalFilesDir(null)!.AbsoluteFile.Path;
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
		ToolbarHandler.Mapper.AppendToMapping("CustomNavigationView", (handler, view) =>
		{
			handler.PlatformView.ContentInsetStartWithNavigation = 0;
		});

		// Remove underscore in Entry Control
		EntryHandler.Mapper.AppendToMapping("NoUnderline", (h, v) =>
		{
			h.PlatformView.BackgroundTintList =
				ColorStateList.ValueOf(Colors.Transparent.ToPlatform());
		});

		// Remove underscore in Editor Control
		EditorHandler.Mapper.AppendToMapping("NoUnderline", (h, v) =>
		{
			h.PlatformView.BackgroundTintList =
				ColorStateList.ValueOf(Colors.Transparent.ToPlatform());
		});
#endif
	}
}
