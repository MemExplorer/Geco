using CommunityToolkit.Maui;
using Geco.Core.Database;
using Geco.Models.DeviceState;
using Geco.Models.DeviceState.StateObservers;
using Geco.Models.Notifications;
using Geco.ViewModels;
using Geco.Views;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Platform;
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
			});

		// page and view model instances
		builder.Services.AddSingleton<AppShellViewModel>();
		builder.Services.AddTransient<ChatPage>();
		builder.Services.AddTransient<ChatViewModel>();
		builder.Services.AddSingleton<SearchPage>();
		builder.Services.AddSingleton<SearchViewModel>();
		builder.Services.AddTransient<SearchResultPage>();
		builder.Services.AddTransient<SearchResultViewModel>();

		// data repository instances
		builder.Services.AddSingleton(new ChatRepository(FileSystem.AppDataDirectory));
		builder.Services.AddSingleton(new TriggerRepository(FileSystem.AppDataDirectory));
		builder.Services.AddSingleton(new PromptRepository(FileSystem.AppDataDirectory));
#if ANDROID
		builder.Services.AddTransient<INotificationManagerService, NotificationManagerService>();

		// monitor service
		builder.Services.AddSingleton<IMonitorManagerService, DeviceUsageMonitorService>();

		// android triggers
		builder.Services.AddSingleton<IDeviceStateObserver, NetworkStateObserver>();
		builder.Services.AddSingleton<IDeviceStateObserver, LocationStateObserver>();
#endif

		// triggers
		builder.Services.AddSingleton<IDeviceStateObserver, BatteryStateObserver>();

#if DEBUG
		builder.Logging.AddDebug();
#endif
		// platform specific modifications
		ApplyAndroidModifications();

		return builder.Build();
	}

	static void ApplyAndroidModifications()
	{
#if ANDROID
		// Adjust header title position
		Microsoft.Maui.Handlers.ToolbarHandler.Mapper.AppendToMapping("CustomNavigationView", (handler, view) =>
		{
			handler.PlatformView.ContentInsetStartWithNavigation = 0;
		});

		// Remove underscore in Entry Control
		Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("NoUnderline", (h, v) =>
		{
			h.PlatformView.BackgroundTintList =
				Android.Content.Res.ColorStateList.ValueOf(Colors.Transparent.ToPlatform());
		});
#endif
	}
}
