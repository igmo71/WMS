using Android.App;
using Android.Content.PM;
using Avalonia;
using Avalonia.Android;
using WMS.Client.Core;
using WMS.Client.Core.Infrastructure;

namespace WMS.Client.Android
{
    [Activity(
        Label = "WMS.Client.Android",
        Theme = "@style/MyTheme.NoActionBar",
        Icon = "@drawable/icon",
        MainLauncher = true,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
    public class MainActivity : AvaloniaMainActivity<App>
    {
        protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
        {
            AppHost.Initialize();
            return base.CustomizeAppBuilder(builder)
                .WithInterFont();
        }
    }
}
