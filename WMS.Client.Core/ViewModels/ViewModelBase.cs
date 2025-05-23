using System.Collections.ObjectModel;
using WMS.Client.Core.Infrastructure;
using WMS.Client.Core.Services;

namespace WMS.Client.Core.ViewModels
{
    internal abstract class ViewModelBase : SafeBindable
    {
        internal virtual string Title => "Unknown";
        internal virtual bool Persistent => false;

        internal RelayCommand CloseCommand { get; }
        internal ObservableCollection<RelayCommand> Commands { get; } = new();

        protected ViewModelBase() => CloseCommand = new RelayCommand((p) => AppHost.GetService<NavigationService>().ClosePage(this), (p) => !Persistent);

        internal virtual void OnCreate()
        {
            AppHost.GetService<BarcodeScannerService>().BarcodeScanned += BarcodeScanned;
        }

        internal virtual void OnClose()
        {
            AppHost.GetService<BarcodeScannerService>().BarcodeScanned -= BarcodeScanned;
        }

        internal virtual void OnActivate()
        {

        }

        internal virtual void OnDeactivate()
        {

        }

        private void BarcodeScanned(object? sender, BarcodeScannedEventArgs e)
        {
            if (AppHost.GetService<NavigationService>().Current == this)
                ProcessBarcode(e.Barcode);
        }

        protected virtual void ProcessBarcode(string barcode)
        {

        }
    }
}
