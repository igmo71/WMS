using System;
using System.Collections.Generic;
using WMS.Shared.Models;

namespace WMS.Client.Core.Interfaces
{
    internal interface IEntityRepository
    {
        internal EntityBase GetById(Guid id);
        internal IEnumerable<EntityBase> GetList();
        internal void Add(EntityBase entity);
        internal void Delete(EntityBase entity);
        internal void Update(EntityBase entity);
    }
}
