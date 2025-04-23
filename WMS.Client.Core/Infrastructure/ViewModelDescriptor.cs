using System;
using WMS.Client.Core.ViewModels;

namespace WMS.Client.Core.Infrastructure
{
    internal readonly struct ViewModelDescriptor
    {
        public string UniqueKey { get; }
        public Func<ViewModelBase> Factory { get; }

        public ViewModelDescriptor(string uniqueKey, Func<ViewModelBase> factory)
        {
            UniqueKey = uniqueKey;
            Factory = factory;
        }
    }
}
