using System;
using System.Collections.Generic;
using WMS.Client.Core.Interfaces;
using WMS.Shared.Models;

namespace WMS.Client.Core.Repositories
{
    internal class CustomEntityRepository : IEntityRepository
    {
        private readonly Type _type;
        private readonly Func<Guid, EntityBase> _getById;
        private readonly Func<IEnumerable<EntityBase>> _getList;
        private readonly Action<EntityBase> _add;
        private readonly Action<EntityBase> _delete;
        private readonly Action<EntityBase> _update;

        Type IEntityRepository.Type => _type;

        internal CustomEntityRepository(Type type,
            Func<Guid, EntityBase> getById,
            Func<IEnumerable<EntityBase>> getList,
            Action<EntityBase> add,
            Action<EntityBase> delete,
            Action<EntityBase> update)
        {
            _type = type;
            _getById = getById;
            _getList = getList;
            _add = add;
            _delete = delete;
            _update = update;
        }

        EntityBase IEntityRepository.GetById(Guid id) => _getById.Invoke(id);

        IEnumerable<EntityBase> IEntityRepository.GetList() => _getList.Invoke();

        void IEntityRepository.Add(EntityBase entity) => _add.Invoke(entity);

        void IEntityRepository.Delete(EntityBase entity) => _delete.Invoke(entity);

        void IEntityRepository.Update(EntityBase entity) => _update.Invoke(entity);
    }
}
