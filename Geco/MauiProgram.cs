using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;
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
        if (DeviceInfo.Platform == DevicePlatform.Android)
        {
            // Adjust header title position
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

            EntryHandler.Mapper.AppendToMapping(nameof(Entry), (handler, view) =>
            {
                // Remove underline from entry controls
                /* Orignal Code: 
                 * 
                 *   //handler.PlatformView.BackgroundTintList = ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
                 *   //handler.PlatformView.SetHintTextColor(ColorStateList.ValueOf(Android.Graphics.Color.Red));
                 *   
                 *   Hopefully, the resolve issue gets fixed in the .NET 9 release.
                 *   For now, we stick with this temporary and hacky reflection solution.
                 */

                if (view is Entry)
                {
                    var platformView = handler.PlatformView;
                    var platformType = platformView.GetType();
                    var bgTintListProperty = platformType.GetProperty("BackgroundTintList");
                    var colorStateList = bgTintListProperty?.GetGetMethod()?.ReturnType;
                    var setHintTextColorFunc = platformType.GetMethod("SetHintTextColor", [colorStateList!]);
                    var colorStateValueOfFunc = colorStateList?.GetMethods().First(x => x.Name == "ValueOf");

                    // transparent color in current platform format
                    var currPlatformTransparentColor = Color.FromUint(0).ToPlatform();
                    var androidTransparentColor = colorStateValueOfFunc?.Invoke(null, [currPlatformTransparentColor]);

                    // hide underline here
                    bgTintListProperty?.SetValue(platformView, androidTransparentColor);
                    setHintTextColorFunc?.Invoke(platformView, [androidTransparentColor]);
                }
            });
        }
    }
}
