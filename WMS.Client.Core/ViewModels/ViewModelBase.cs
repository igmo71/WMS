﻿using WMS.Client.Core.Infrastructure;

namespace WMS.Client.Core.ViewModels
{
    internal abstract class ViewModelBase : SafeBindable
    {
        internal string Name { get; set; }
    }
}
