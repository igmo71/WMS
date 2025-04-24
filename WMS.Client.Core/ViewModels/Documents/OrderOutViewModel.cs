using System;
using System.Collections.ObjectModel;
using WMS.Client.Core.Interfaces;
using WMS.Client.Core.Repositories;
using WMS.Shared.Models.Catalogs;
using WMS.Shared.Models.Documents;

namespace WMS.Client.Core.ViewModels.Documents
{
    internal class OrderOutViewModel : ViewModelBase
    {
        private readonly OrderOut _model;
        private readonly ObservableCollection<OrderOutProduct> _products = new ObservableCollection<OrderOutProduct>();
        private readonly IEntityRepository productsRepository = EntityRepositoryFactory.Get<Product>();

        internal override string Title => $"{nameof(OrderOut)} {_model.Number} {_model.DateTime}";
        internal OrderOut Model => _model;
        internal ObservableCollection<OrderOutProduct> Products => _products;

        internal OrderOutViewModel(OrderOut model)
        {
            _model = model;
            UpdateProducts();
        }

        private void UpdateProducts()
        {
            _products.Clear();
            _model.Products.ForEach((p) => _products.Add(new OrderOutProduct() { Product = productsRepository.GetById(p.ProductId) as Product ?? throw new ArgumentException(), Count = p.Count }));
        }

        internal class OrderOutProduct
        {
            internal Product Product { get; set; }
            internal double Count { get; set; }
        }
    }
}
