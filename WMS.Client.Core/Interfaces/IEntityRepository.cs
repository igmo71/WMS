using System;
using System.Collections.Generic;
using WMS.Shared.Models;

namespace WMS.Client.Core.Interfaces
{
    internal interface IEntityRepository
    {
        event EventHandler<EntityChangedEventArgs> EntityCreated;
        event EventHandler<EntityChangedEventArgs> EntityDeleted;
        event EventHandler<EntityChangedEventArgs> EntityUpdated;

        internal Type Type { get; }
        internal EntityBase GetById(Guid id);
        internal IEnumerable<EntityBase> GetList();
        internal EntityBase Create(EntityBase entity);
        internal void Delete(EntityBase entity);
        internal void Update(EntityBase entity);

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
