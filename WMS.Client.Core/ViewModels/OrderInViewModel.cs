using System;
using System.Collections.ObjectModel;
using WMS.Client.Core.Interfaces;
using WMS.Client.Core.Repositories;
using WMS.Shared.Models.Catalogs;
using WMS.Shared.Models.Documents;

namespace WMS.Client.Core.ViewModels
{
    internal class OrderInViewModel : ViewModelBase
    {
        private readonly OrderIn _model;
        private readonly ObservableCollection<Product> _products = new ObservableCollection<Product>();
        private readonly IEntityRepository productsRepository = EntityRepositoryFactory.Get<Product>();

        internal OrderIn Model => _model;
        internal ObservableCollection<Product> Products => _products;

        internal OrderInViewModel(OrderIn model)
        {
            _model = model;
            Name = $"{nameof(OrderIn)} {_model.Number} {_model.DateTime}";
            UpdateProducts();
        }

        private void UpdateProducts()
        {
            _products.Clear();
            _model.Products.ForEach((p) =>
                _products.Add(productsRepository.GetById(p.ProductId) as Product ?? throw new ArgumentException()));
        }
    }
}
