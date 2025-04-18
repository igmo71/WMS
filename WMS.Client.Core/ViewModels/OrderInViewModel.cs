using System;
using System.Collections.ObjectModel;
using WMS.Client.Core.Infrastructure;
using WMS.Client.Core.Interfaces;
using WMS.Client.Core.Repositories;
using WMS.Shared.Models.Catalogs;
using WMS.Shared.Models.Documents;

namespace WMS.Client.Core.ViewModels
{
    internal class OrderInViewModel : ViewModelBase
    {
        private readonly OrderIn _model;
        private readonly ObservableCollection<OrderInProduct> _products = new ObservableCollection<OrderInProduct>();
        private readonly IEntityRepository productsRepository = EntityRepositoryFactory.Get<Product>();

        internal override string Name => $"{nameof(OrderIn)} {_model.Number} {_model.DateTime}";
        internal OrderIn Model => _model;
        internal ObservableCollection<OrderInProduct> Products => _products;

        internal RelayCommand SaveCommand { get; }

        internal OrderInViewModel(OrderIn model)
        {
            _model = model;
            UpdateProducts();

            SaveCommand = new RelayCommand(p => EntityRepositoryFactory.Get<OrderIn>().Update(_model));
        }

        private void UpdateProducts()
        {
            _products.Clear();
            _model.Products.ForEach((p) => _products.Add(new OrderInProduct() { Product = productsRepository.GetById(p.ProductId) as Product ?? throw new ArgumentException(), Count = p.Count }));
        }

        internal class OrderInProduct
        {
            internal Product Product { get; set; }
            internal double Count { get; set; }
        }
    }
}
