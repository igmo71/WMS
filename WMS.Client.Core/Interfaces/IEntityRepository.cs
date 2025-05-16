using System;
using System.Collections.Generic;
using WMS.Client.Core.Adapters;
using WMS.Shared.Models;

namespace WMS.Client.Core.Interfaces
{
    internal interface IEntityRepository
    {
        event EventHandler<EntityChangedEventArgs> EntityCreated;
        event EventHandler<EntityChangedEventArgs> EntityDeleted;
        event EventHandler<EntityChangedEventArgs> EntityUpdated;

        internal Type Type { get; }
        internal IEnumerable<EntityBase> GetList();
        internal EntityAdapter GetById(Guid id);
        internal Guid CreateOrUpdate(EntityAdapter adapter);
        internal void Delete(Guid id);
    }

    internal class EntityChangedEventArgs : EventArgs
    {
        public EntityBase Entity { get; }

        public EntityChangedEventArgs(EntityBase entity)
        {
            Entity = entity;
        }
    }
}
