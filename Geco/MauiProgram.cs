using CommunityToolkit.Maui;
using Geco.Core.Database;
using Geco.ViewModels;
using Geco.Views;
using InputKit.Handlers;
using InputKit.Shared.Controls;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Platform;

namespace Geco;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
				fonts.AddFont("Poppins-Regular.ttf", "Poppins");
				fonts.AddFont("FontAwesome6DuotoneNew-Solid.ttf", "FontAwesome");
			})
			.ConfigureMauiHandlers(handlers =>
			{
				handlers.AddInputKitHandlers();
			});

		// page and view model instances
		builder.Services.AddSingleton<AppShellViewModel>();
		builder.Services.AddTransient<ChatPage>();
		builder.Services.AddTransient<ChatViewModel>();

		// data repository instances
		builder.Services.AddSingleton<ChatRepository>();

		// settings
		SelectionView.GlobalSetting.CornerRadius = 10;

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
