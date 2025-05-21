using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using WMS.Client.Core.Adapters.Catalogs;
using WMS.Client.Core.Infrastructure;
using WMS.Client.Core.Interfaces;
using WMS.Client.Core.Repositories;
using WMS.Shared.Models;
using WMS.Shared.Models.Catalogs;
using WMS.Shared.Models.Documents;

namespace WMS.Client.Core.Adapters.Documents
{
    internal class OrderInAdapter : DocumentAdapter
    {
        private ObservableCollection<OrderInProduct> _products = new();

        public ObservableCollection<OrderInProduct> Products => _products;

        public OrderInAdapter(EntityBase entity) : base(entity)
        {
            if (entity is not OrderIn)
                throw new ArgumentException();

            UpdateAdapter(entity as OrderIn ?? throw new InvalidCastException());
            EntityRepositoryFactory.Get<OrderIn>().EntityUpdated += EntityUpdated;
        }

        private void EntityUpdated(object? sender, EntityUpdatedEventArgs e)
        {
            if (e.Entity is OrderIn orderIn && orderIn.Id == _id)
                UpdateAdapter(orderIn);
        }

        internal override void Save() => EntityRepositoryFactory.Get<OrderIn>().CreateOrUpdate(this);

        internal override EntityBase GetEntity()
        {
            OrderIn entity = new OrderIn()
            {
                Id = Id,
                Name = "",
                Number = Number,
                DateTime = DateTime,
                Products = new List<OrderIn.OrderInProduct>()
            };

            _products.ToList().ForEach(p => entity.Products.Add(new OrderIn.OrderInProduct() { ProductId = p.Product.Id, Count = p.Count }));

            return entity;
        }

        private void UpdateAdapter(OrderIn entity)
        {
            InvokeUI(() =>
            {
                _products.Clear();
                entity.Products.ForEach(p =>
                {
                    _products.Add(new OrderInProduct()
                    {
                        Product = EntityRepositoryFactory.Get<Product>().GetById(p.ProductId) as ProductAdapter ?? throw new InvalidCastException(),
                        Count = p.Count
                    });
                });
            });
        }

        internal void AddProduct(string barcode)
        {
            ProductAdapter? product = EntityRepositoryFactory.Get<Product>().GetById(new Guid(barcode)) as ProductAdapter;
            if (product == null)
                return;

            InvokeUI(() =>
            {
                OrderInProduct row = _products.FirstOrDefault(r => r.Product == product);
                if (row == null)
                {
                    row = new OrderInProduct() { Product = product, Count = 0 };
                    _products.Add(row);
                }

                row.Count++;
            });
        }

        internal class OrderInProduct : SafeBindable
        {
            private ProductAdapter _product;
            private double _count;

            internal ProductAdapter Product { get => LockAndGet(ref _product); set => SetAndNotify(ref _product, value); }
            internal double Count { get => LockAndGet(ref _count); set => SetAndNotify(ref _count, value); }
        }

    }
}
