using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;

namespace Geco
{
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
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif
            // platform specific modifications
            ApplyAndroidModifications();

            return builder.Build();
        }

        static void ApplyAndroidModifications()
        {
            ToolbarHandler.Mapper.AppendToMapping("CustomNavigationView", static (handler, view) =>
            {
                if (DeviceInfo.Platform == DevicePlatform.Android)
                {
                    // Original Code: handler.PlatformView.ContentInsetStartWithNavigation = 0;
                    // Compiler complains about not resolving ContentInsetStartWithNavigation in .NET 9 Preview.
                    // Check in december again if it's still an issue when .NET 9 release is out
                    // For now, we cope with this hack solution
                    var platform = handler.PlatformView;
                    var propertyItem = platform.GetType().GetProperty("ContentInsetStartWithNavigation");
                    propertyItem?.SetValue(platform, 0);
                }
            });
        }
    }
}
