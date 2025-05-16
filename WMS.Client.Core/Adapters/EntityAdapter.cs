using System;
using WMS.Client.Core.Infrastructure;
using WMS.Client.Core.Interfaces;
using WMS.Shared.Models;

namespace WMS.Client.Core.Adapters
{
    internal abstract class EntityAdapter : SafeBindable
    {
        protected Guid _id;

        internal Guid Id => _id;
        internal Type Type { get; }
        internal bool IsNew => _id == Guid.Empty;

        internal EntityAdapter(EntityBase entity)
        {
            _id = entity.Id;
            Type = entity.GetType();
        }

        internal abstract EntityBase GetEntity();

        internal abstract void Save();

        protected void InvokeUI(Action action) => AppHost.GetService<IUIService>().InvokeUIThread(action);
    }
}
