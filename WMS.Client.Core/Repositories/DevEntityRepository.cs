using System;
using System.Collections.Generic;
using WMS.Client.Core.Adapters;
using WMS.Client.Core.Interfaces;
using WMS.Shared.Models;

namespace WMS.Client.Core.Repositories
{
    internal class DevEntityRepository<TEntity> : IEntityRepository where TEntity : EntityBase
    {
        public event EventHandler<EntityChangedEventArgs> EntityCreated;
        public event EventHandler<EntityChangedEventArgs> EntityDeleted;
        public event EventHandler<EntityChangedEventArgs> EntityUpdated;

        Type IEntityRepository.Type => typeof(TEntity);

        public DevEntityRepository() => Initialize();

        EntityAdapter IEntityRepository.GetById(Guid id) => throw new NotImplementedException();

        IEnumerable<EntityBase> IEntityRepository.GetList() => throw new NotImplementedException();

        Guid IEntityRepository.CreateOrUpdate(EntityAdapter adapter) => throw new NotImplementedException();

        void IEntityRepository.Delete(Guid id) => throw new NotImplementedException();

        private void Initialize()
        {

        }
    }
}
