using System;
using System.ComponentModel.Design;
using WMS.Client.Core.Interfaces;
using WMS.Client.Core.Services;

namespace WMS.Client.Core.Infrastructure
{
    public static class AppHost
    {
        private static readonly ServiceContainer _services = new ServiceContainer();

        public static void Initialize()
        {
            _services.AddService(typeof(HTTPClientService), new HTTPClientService());
            _services.AddService(typeof(SignalRClientService), new SignalRClientService());
            _services.AddService(typeof(IUIService), new AvaloniaUIService());
            _services.AddService(typeof(BarcodeScannerService), new BarcodeScannerService());
            _services.AddService(typeof(NavigationService), new NavigationService());
        }

        public static T GetService<T>() where T : class
        {
            T service = _services.GetService(typeof(T)) as T;
            if (service == null)
                throw new InvalidOperationException();

            return service;
        }

        public static void Dispose() => _services.Dispose();
    }
}
