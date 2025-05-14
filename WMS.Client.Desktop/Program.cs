using System;
using Avalonia;
using WMS.Client.Core;
using WMS.Client.Core.Infrastructure;

namespace WMS.Client.Desktop
{
    internal sealed class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            AppHost.Initialize();
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            AppHost.Dispose();
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}
