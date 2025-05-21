using System.Collections.Generic;
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
        private static readonly Dictionary<Key, (char normal, char shifted)> _keyMap = new()
        {
            [Key.A] = ('a', 'A'), [Key.B] = ('b', 'B'), [Key.C] = ('c', 'C'), [Key.D] = ('d', 'D'),
            [Key.E] = ('e', 'E'), [Key.F] = ('f', 'F'), [Key.G] = ('g', 'G'), [Key.H] = ('h', 'H'),
            [Key.I] = ('i', 'I'), [Key.J] = ('j', 'J'), [Key.K] = ('k', 'K'), [Key.L] = ('l', 'L'),
            [Key.M] = ('m', 'M'), [Key.N] = ('n', 'N'), [Key.O] = ('o', 'O'), [Key.P] = ('p', 'P'),
            [Key.Q] = ('q', 'Q'), [Key.R] = ('r', 'R'), [Key.S] = ('s', 'S'), [Key.T] = ('t', 'T'),
            [Key.U] = ('u', 'U'), [Key.V] = ('v', 'V'), [Key.W] = ('w', 'W'), [Key.X] = ('x', 'X'),
            [Key.Y] = ('y', 'Y'), [Key.Z] = ('z', 'Z'),

            [Key.D0] = ('0', ')'), [Key.D1] = ('1', '!'), [Key.D2] = ('2', '@'), [Key.D3] = ('3', '#'),
            [Key.D4] = ('4', '$'), [Key.D5] = ('5', '%'), [Key.D6] = ('6', '^'), [Key.D7] = ('7', '&'),
            [Key.D8] = ('8', '*'), [Key.D9] = ('9', '('),

            [Key.Space] = (' ', ' '), [Key.OemMinus] = ('-', '_'), [Key.OemPlus] = ('=', '+'), [Key.Oem4] = ('[', '{'),
            [Key.Oem6] = (']', '}'), [Key.Oem5] = ('\\', '|'), [Key.Oem1] = (';', ':'), [Key.Oem7] = ('\'', '"'),
            [Key.OemComma] = (',', '<'), [Key.OemPeriod] = ('.', '>'), [Key.Oem2] = ('/', '?'), [Key.Oem3] = ('`', '~'),
        };

        public override void Initialize() => AvaloniaXamlLoader.Load(this);

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                DisableAvaloniaDataAnnotationValidation();
                desktop.MainWindow = new MainWindow();
                desktop.MainWindow.DataContext = new MainViewModel();
                desktop.MainWindow.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel, true);
            }
            else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
            {
                singleViewPlatform.MainView = new MainView();
                singleViewPlatform.MainView.DataContext = new MainViewModel();
                singleViewPlatform.MainView.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel, true);
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                AppHost.GetService<BarcodeScannerService>().AddReturn();
            else if (_keyMap.TryGetValue(e.Key, out (char normal, char shifted) value))
                AppHost.GetService<BarcodeScannerService>().AddText((e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? value.shifted : value.normal).ToString());
        }

        private void DisableAvaloniaDataAnnotationValidation()
        {
            foreach (var plugin in BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray())
                BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}