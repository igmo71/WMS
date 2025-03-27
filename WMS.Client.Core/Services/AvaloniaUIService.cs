using System;
using Avalonia.Threading;
using WMS.Client.Core.Interfaces;

namespace WMS.Client.Core.Services
{
    internal class AvaloniaUIService : IUIService
    {
        public void InvokeUIThread(Action action)
        {
            if (Dispatcher.UIThread.CheckAccess())
                action();
            else
                Dispatcher.UIThread.InvokeAsync(() => action());
        }
    }
}
