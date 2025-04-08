using System;
using System.Collections.Generic;
using WMS.Client.Core.Interfaces;
using WMS.Client.Core.Services;
using WMS.Shared.Models;
using WMS.Shared.Models.Documents;

namespace WMS.Client.Core.Repositories
{
    internal class RemoteEntityRepository : IEntityRepository
    {
        private readonly Func<Guid, EntityBase> _getById;
        private readonly Func<IEnumerable<EntityBase>> _getList;

        internal Type Type { get; private set; }

        public RemoteEntityRepository(Type type)
        {
            Type = type;

            if (type == typeof(OrderIn))
            {
                _getById = HTTPService.GetObject<OrderIn>;
                _getList = HTTPService.GetList<OrderIn>;

                return;
            }

            throw new NotSupportedException();
        }

        EntityBase IEntityRepository.GetById(Guid id) => _getById.Invoke(id);

        IEnumerable<EntityBase> IEntityRepository.GetList() => _getList.Invoke();

        void IEntityRepository.Add(EntityBase entity) => throw new NotImplementedException();

        void IEntityRepository.Delete(EntityBase entity) => throw new NotImplementedException();

        void IEntityRepository.Update(EntityBase entity) => throw new NotImplementedException();
    }
}
