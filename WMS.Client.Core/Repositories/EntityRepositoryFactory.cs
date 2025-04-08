using System;
using System.Collections.Concurrent;
using WMS.Client.Core.Services;
using WMS.Shared.Models;

namespace WMS.Client.Core.Repositories
{
    internal static class EntityRepositoryFactory
    {
        private readonly static ConcurrentDictionary<(Type, RepositoryKind), EntityRepository> _cache = new ConcurrentDictionary<(Type, RepositoryKind), EntityRepository>();
        private static RepositoryKind _kind = RepositoryKind.Remote;

        internal static EntityRepository Get<TEntity>() where TEntity : EntityBase
        {
            EntityRepository repository = _cache.GetOrAdd((typeof(TEntity), _kind), (k) => CreateNew<TEntity>());
            if (repository == null)
                throw new NotSupportedException();

            return repository;
        }

        private static EntityRepository CreateNew<TEntity>() where TEntity : EntityBase
        {
            switch (_kind)
            {
                case RepositoryKind.Remote:
                    return CreateRemote<TEntity>();
                case RepositoryKind.Combined:
                    return CreateCombined<TEntity>();
                case RepositoryKind.Dev:
                    return CreateDev<TEntity>();
                default:
                    throw new NotSupportedException();
            }
        }

        private static EntityRepository CreateRemote<TEntity>() where TEntity : EntityBase
        {
            return new EntityRepository(typeof(TEntity),
                (id) => HTTPService.GetObject<TEntity>(id),
                () => HTTPService.GetList<TEntity>(),
                (e) => throw new NotImplementedException(),
                (e) => throw new NotImplementedException(),
                (e) => throw new NotImplementedException());

            throw new NotSupportedException();
        }

        private static EntityRepository CreateCombined<TEntity>() where TEntity : EntityBase
        {
            throw new NotImplementedException();
        }

        private static EntityRepository CreateDev<TEntity>() where TEntity : EntityBase
        {
            throw new NotImplementedException();
        }
    }

    internal enum RepositoryKind
    {
        Remote,
        Combined,
        Dev
    }
}
