using System.Threading.Tasks;
using Avalonia;
using Avalonia.Browser;
using WMS.Client.Core;
using WMS.Client.Core.Infrastructure;

internal sealed partial class Program
{
    private static Task Main(string[] args)
    {
        AppHost.Initialize();
        return BuildAvaloniaApp().WithInterFont().StartBrowserAppAsync("out");
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>();
}