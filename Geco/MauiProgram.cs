using CommunityToolkit.Maui;
#if ANDROID
using Geco.ActionObservers;
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
			.InitializeAndroidServices();

#if DEBUG
		builder.Logging.AddDebug();
#endif
		// platform specific modifications
		ApplyAndroidUiModifications();

		return builder.Build();
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
		builder.Services.AddSingleton(new ChatRepository(FileSystem.AppDataDirectory));
		builder.Services.AddSingleton(new TriggerRepository(FileSystem.AppDataDirectory));
		builder.Services.AddSingleton(new PromptRepository(FileSystem.AppDataDirectory));
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
#endif
	}
}
