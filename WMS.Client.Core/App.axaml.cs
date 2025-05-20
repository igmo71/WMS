using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using WMS.Client.Core.Infrastructure;
using WMS.Client.Core.Services;
using WMS.Client.Core.ViewModels;
using WMS.Client.Core.Views;

namespace WMS.Client.Core
{
    public partial class App : Application
    {
        public override void Initialize() => AvaloniaXamlLoader.Load(this);

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                DisableAvaloniaDataAnnotationValidation();
                desktop.MainWindow = new MainWindow();
                desktop.MainWindow.DataContext = new MainViewModel();
                desktop.MainWindow.AddHandler(InputElement.TextInputEvent, OnTextInput, RoutingStrategies.Tunnel, true);
            }
            else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
            {
                singleViewPlatform.MainView = new MainView();
                singleViewPlatform.MainView.DataContext = new MainViewModel();
                singleViewPlatform.MainView.AddHandler(InputElement.TextInputEvent, OnTextInput, RoutingStrategies.Tunnel, true);
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void OnTextInput(object? sender, TextInputEventArgs e) => AppHost.GetService<BarcodeScannerService>().Add(e.Text);

        private void DisableAvaloniaDataAnnotationValidation()
        {
            foreach (var plugin in BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray())
                BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}