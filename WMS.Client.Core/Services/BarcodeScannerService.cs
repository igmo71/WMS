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

        internal void Add(string text)
        {
            if (_stopwatch.ElapsedMilliseconds >= 100)
                _barcode = string.Empty;

            _barcode += text;
            if (_barcode.EndsWith(Environment.NewLine))
            {
                Task.Run(() => BarcodeScanned?.Invoke(this, new BarcodeScannedEventArgs(_barcode)));
                _barcode = string.Empty;
            }

            _stopwatch.Restart();
        }
    }

    internal class BarcodeScannedEventArgs : EventArgs
    {
        internal string Barcode { get; }

        public BarcodeScannedEventArgs(string barcode) => Barcode = barcode;
    }
}
