using System;
using System.Collections.Concurrent;
using WMS.Client.Core.Interfaces;
using WMS.Client.Core.Services;
using WMS.Shared.Models;

namespace WMS.Client.Core.Repositories
{
    internal static class EntityRepositoryFactory
    {
        private readonly static ConcurrentDictionary<(Type, RepositoryKind), IEntityRepository> _cache = new ConcurrentDictionary<(Type, RepositoryKind), IEntityRepository>();
        private static RepositoryKind _kind = RepositoryKind.Dev;

        internal static IEntityRepository Get<TEntity>() where TEntity : EntityBase
        {
            return _cache.GetOrAdd((typeof(TEntity), _kind), (k) => CreateNew<TEntity>()) ?? throw new NotSupportedException();
        }

        private static IEntityRepository CreateNew<TEntity>() where TEntity : EntityBase
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

        private static IEntityRepository CreateRemote<TEntity>() where TEntity : EntityBase
        {
            return new CustomEntityRepository(typeof(TEntity),
                (id) => HTTPService.GetObject<TEntity>(id),
                () => HTTPService.GetList<TEntity>(),
                (e) => throw new NotImplementedException(),
                (e) => throw new NotImplementedException(),
                (e) => throw new NotImplementedException());

            throw new NotSupportedException();
        }

        private static IEntityRepository CreateCombined<TEntity>() where TEntity : EntityBase => throw new NotImplementedException();

        private static IEntityRepository CreateDev<TEntity>() where TEntity : EntityBase => new DevEntityRepository(typeof(TEntity));
    }

    internal enum RepositoryKind
    {
        Remote,
        Combined,
        Dev
    }
}
