using System;
using System.Collections.Generic;
using System.Linq;
using WMS.Client.Core.Interfaces;
using WMS.Shared.Models;
using WMS.Shared.Models.Catalogs;
using WMS.Shared.Models.Documents;

namespace WMS.Client.Core.Repositories
{
    internal class DevEntityRepository<TEntity> : IEntityRepository where TEntity : EntityBase
    {
        private readonly List<TEntity> _entities = new List<TEntity>();

        public event EventHandler<EntityChangedEventArgs> EntityCreated;
        public event EventHandler<EntityChangedEventArgs> EntityDeleted;
        public event EventHandler<EntityChangedEventArgs> EntityUpdated;

        Type IEntityRepository.Type => typeof(TEntity);

        public DevEntityRepository() => Initialize();

        EntityBase IEntityRepository.GetById(Guid id) => _entities.Where((e) => e.Id == id).FirstOrDefault() ?? throw new KeyNotFoundException();

        IEnumerable<EntityBase> IEntityRepository.GetList() => _entities.ToArray();

        void IEntityRepository.Create(EntityBase entity)
        {
            if (entity is TEntity item)
            {
                if (_entities.Any(e => e.Id == item.Id))
                    throw new InvalidOperationException();

                _entities.Add(item);
                EntityCreated?.Invoke(this, new EntityChangedEventArgs(entity));
            }
            else
                throw new ArgumentException();
        }

        void IEntityRepository.Delete(EntityBase entity)
        {
            if (entity is TEntity item)
            {
                int removed = _entities.RemoveAll(e => e.Id == item.Id);
                if (removed == 0)
                    throw new KeyNotFoundException();

                if (removed > 0)
                    EntityDeleted?.Invoke(this, new EntityChangedEventArgs(item));
            }
            else
                throw new ArgumentException();
        }

        void IEntityRepository.Update(EntityBase entity)
        {
            if (entity is TEntity item)
            {
                int index = _entities.FindIndex(e => e.Id == item.Id);
                if (index < 0)
                    throw new KeyNotFoundException();

                _entities[index] = item;
                EntityUpdated?.Invoke(this, new EntityChangedEventArgs(item));
            }
            else
                throw new ArgumentException();
        }

        private void Initialize()
        {
            if (typeof(TEntity) == typeof(OrderIn))
            {
                Random random = new Random(Guid.NewGuid().GetHashCode());
                List<Product> productsRepository = EntityRepositoryFactory.Get<Product>().GetList().OfType<Product>().ToList();

                for (int i = 0; i < 100; i++)
                {
                    List<OrderInProduct> orderProducts = new List<OrderInProduct>();
                    for(int j = 0; j < random.Next(1, 100); j++)
                        orderProducts.Add(new OrderInProduct()
                        {
                            Count = random.Next(1, 10),
                            ProductId = productsRepository.ElementAt(random.Next(0, productsRepository.Count - 1)).Id
                        });

                    _entities.Add(new OrderIn()
                    {
                        Id = Guid.NewGuid(),
                        DateTime = DateTime.UtcNow,
                        Name = "Order Out",
                        Number = $"NUM-{random.Next(1000, 9999)}",
                        Products = orderProducts
                    } as TEntity ?? throw new InvalidCastException());
                }
            }

            if (typeof(TEntity) == typeof(OrderOut))
            {
                Random random = new Random(Guid.NewGuid().GetHashCode());
                List<Product> productsRepository = EntityRepositoryFactory.Get<Product>().GetList().OfType<Product>().ToList();

                for (int i = 0; i < 100; i++)
                {
                    List<OrderOutProduct> orderProducts = new List<OrderOutProduct>();
                    for (int j = 0; j < random.Next(1, 100); j++)
                        orderProducts.Add(new OrderOutProduct()
                        {
                            Count = random.Next(1, 10),
                            ProductId = productsRepository.ElementAt(random.Next(0, productsRepository.Count - 1)).Id
                        });

                    _entities.Add(new OrderOut()
                    {
                        Id = Guid.NewGuid(),
                        DateTime = DateTime.UtcNow,
                        Name = "Order In",
                        Number = $"NUM-{random.Next(1000, 9999)}",
                        Products = orderProducts
                    } as TEntity ?? throw new InvalidCastException());
                }
            }

            if (typeof(TEntity) == typeof(Product))
            {
                Random random = new Random(Guid.NewGuid().GetHashCode());
                for (int i = 0; i < random.Next(1, 200); i++)
                    _entities.Add(new Product() { Id = Guid.NewGuid(), Name = $"Product {i}" } as TEntity ?? throw new InvalidCastException());
            }
        }
    }
}
