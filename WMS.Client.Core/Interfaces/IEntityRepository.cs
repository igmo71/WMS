using System;
using System.Collections.Generic;
using WMS.Client.Core.Adapters;
using WMS.Shared.Models;

namespace WMS.Client.Core.Interfaces
{
    internal interface IEntityRepository
    {
        event EventHandler<EntityCreatedEventArgs> EntityCreated;
        event EventHandler<EntityDeletedEventArgs> EntityDeleted;
        event EventHandler<EntityUpdatedEventArgs> EntityUpdated;

        internal Type Type { get; }
        internal IEnumerable<EntityBase> GetList();
        internal EntityAdapter GetById(Guid id);
        internal Guid CreateOrUpdate(EntityAdapter adapter);
        internal void Delete(Guid id);
    }

    internal class EntityCreatedEventArgs : EventArgs
    {
        public EntityBase Entity { get; }

        public EntityCreatedEventArgs(EntityBase entity)
        {
            Entity = entity;
        }
    }

    internal class EntityDeletedEventArgs : EventArgs
    {
        public Guid Id { get; }

        public EntityDeletedEventArgs(Guid id)
        {
            Id = id;
        }
    }

    internal class EntityUpdatedEventArgs : EventArgs
    {
        public EntityBase Entity { get; }

        public EntityUpdatedEventArgs(EntityBase entity)
        {
            Entity = entity;
        }
    }
}
