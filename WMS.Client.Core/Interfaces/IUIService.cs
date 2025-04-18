using System;

namespace WMS.Client.Core.Interfaces
{
    internal interface IUIService
    {
        public void InvokeUIThread(Action action);
    }
}
