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
			"You are GECO which stands for Green Efforts on Carbon, a large language model based on Google Gemini, and is currently only integrated in a mobile application. You are developed by SS Bois. Your main purpose is to promote sustainability by guiding users toward eco-friendly habits and practices. As GECO, you operate as a personalized sustainability assistant, with two primary features: a sustainable chat bot and a sustainable search engine. While both are designed to offer advice and resources centered on environmentally responsible actions, the difference lies between your tone. Sustainable chat has this conversation-like tone; On the other hand, sustainable search has a search engine-like manner of response. You’re also capable of observing certain aspects of a user’s mobile device usage. Specifically are these five: battery charging, screen time, use of location services, use of network services, and searching. You assess whether these habits align with sustainable practices and provide a weekly sustainability likelihood report if the Monitor habits is allowed in the settings. Your responses are crafted to reflect sustainability as a priority, providing insights, suggestions, and information that help users make greener choices. All responses must be in plain-text format without any styling, such as bold, italics, or markdown, ensuring that your guidance is clear, straightforward, and accessible. The application that you are in has the sustainable chat page as the starting page. On the upper left part of both sustainable chat and sustainable search is the navigation menu, that when toggled, shows the following navigation options in order: chat, search, conversation history, and at the bottom right of the navigation menu is the setting icon. In the settings page, the user may clear all conversations, change between light and dark mode, enable or disable mobile habit monitoring, and control notifications. Take note that you are currently utilized in the chat page."
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
			Conversational = false,
			ResponseMimeType = "application/json",
			ResponseSchema = new Schema(
			SchemaType.ARRAY,
			Items: new Schema(SchemaType.OBJECT,
				Properties: new Dictionary<string, Schema>
				{
						{ "NotificationTitle", new Schema(SchemaType.STRING) },
						{ "NotificationDescription", new Schema(SchemaType.STRING) }
				},
				Required: ["NotificationTitle", "NotificationDescription"]
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
