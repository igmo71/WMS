using System;
using System.Collections.Generic;
using System.Linq;
using WMS.Client.Core.Interfaces;
using WMS.Shared.Models;
using WMS.Shared.Models.Catalogs;
using WMS.Shared.Models.Documents;

namespace WMS.Client.Core.Repositories
{
    internal class DevEntityRepository : IEntityRepository
    {
        private readonly Type _type;
        private readonly List<EntityBase> _entities = new List<EntityBase>();

        Type IEntityRepository.Type => _type;

        public DevEntityRepository(Type type)
        {
            _type = type;
            Initialize();
        }

        EntityBase IEntityRepository.GetById(Guid id) => _entities.Where((e) => e.Id == id).FirstOrDefault();

        IEnumerable<EntityBase> IEntityRepository.GetList() => _entities.ToArray();

        void IEntityRepository.Add(EntityBase entity) => throw new NotImplementedException();

        void IEntityRepository.Delete(EntityBase entity) => throw new NotImplementedException();

        void IEntityRepository.Update(EntityBase entity) => throw new NotImplementedException();

        private void Initialize()
        {
            if (_type == typeof(OrderIn))
            {
                Random random = new Random();
                List<Product> productsList = EntityRepositoryFactory.Get<Product>().GetList().OfType<Product>().ToList();

                for (int i = 0; i < 100; i++)
                {
                    List<OrderInProduct> products = new List<OrderInProduct>();
                    for(int j = 0; j < random.Next(1, 10); j++)
                        products.Add(new OrderInProduct() { Count = random.Next(1, 10), ProductId = productsList.ElementAt(random.Next(0, productsList.Count - 1)).Id});

                    _entities.Add(new OrderIn() { Id = Guid.NewGuid(), DateTime = DateTime.UtcNow, Name = "Order In", Number = $"NUM-{random.Next(1000, 9999)}", Products = products });
                }
            }

            if (_type == typeof(Product))
            {
                for (int i = 0; i < 100; i++)
                    _entities.Add(new Product() { Id = Guid.NewGuid(), Name = $"Product {i}" });
            }
        }
    }
}
