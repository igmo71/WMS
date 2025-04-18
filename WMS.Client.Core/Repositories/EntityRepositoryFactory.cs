using System;
using System.Collections.Concurrent;
using WMS.Client.Core.Interfaces;
using WMS.Shared.Models;

namespace WMS.Client.Core.Repositories
{
    internal static class EntityRepositoryFactory
    {
        private readonly static ConcurrentDictionary<(Type, RepositoryKind), IEntityRepository> _cache = new ConcurrentDictionary<(Type, RepositoryKind), IEntityRepository>();
        private static RepositoryKind _kind = RepositoryKind.Remote;

        internal static IEntityRepository Get<TEntity>() where TEntity : EntityBase
        {
            return _cache.GetOrAdd((typeof(TEntity), _kind), (k) => Create<TEntity>()) ?? throw new NotSupportedException();
        }

        private static IEntityRepository Create<TEntity>() where TEntity : EntityBase
        {
            switch (_kind)
            {
                case RepositoryKind.Remote:
                    return new RemoteEntityRepository<TEntity>();
                case RepositoryKind.Combined:
                    throw new NotImplementedException();
                case RepositoryKind.Dev:
                    return new DevEntityRepository<TEntity>();
                default:
                    throw new NotSupportedException();
            }
        }
    }

    internal enum RepositoryKind
    {
        Remote,
        Combined,
        Dev
    }
}
