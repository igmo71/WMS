using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace WMS.Client.Core.Services
{
    internal class BarcodeScannerService
    {
        private readonly Stopwatch _stopwatch = new();
        private string _barcode = string.Empty;

        public event EventHandler<BarcodeScannedEventArgs>? BarcodeScanned;

        internal void AddText(string text)
        {
            if (_stopwatch.ElapsedMilliseconds >= 50)
                _barcode = string.Empty;

            _barcode += text;
            _stopwatch.Restart();
        }

        internal void AddReturn()
        {
            if (_stopwatch.ElapsedMilliseconds >= 50)
                _barcode = string.Empty;

            if (_barcode != string.Empty)
            {
                BarcodeScannedEventArgs args = new BarcodeScannedEventArgs(_barcode);
                Task.Run(() => BarcodeScanned?.Invoke(this, args));
                _barcode = string.Empty;
            }
        }
    }

    internal class BarcodeScannedEventArgs : EventArgs
    {
        internal string Barcode { get; }

        public BarcodeScannedEventArgs(string barcode) => Barcode = barcode;
    }
}
