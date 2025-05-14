using System;
using System.Collections.ObjectModel;
using System.Linq;
using WMS.Client.Core.Infrastructure;
using WMS.Client.Core.Interfaces;
using WMS.Client.Core.Repositories;
using WMS.Shared.Models.Catalogs;
using WMS.Shared.Models.Documents;

namespace WMS.Client.Core.ViewModels.Documents
{
    internal class OrderInViewModel : ViewModelBase
    {
        private OrderIn _model;
        private readonly ObservableCollection<OrderInProduct> _products = new ObservableCollection<OrderInProduct>();
        private readonly IEntityRepository _repository = EntityRepositoryFactory.Get<OrderIn>();
        private readonly IEntityRepository _productsRepository = EntityRepositoryFactory.Get<Product>();

        internal override string Title => $"{nameof(OrderIn)} {_model.Number} {_model.DateTime}";
        internal OrderIn Model => _model;
        internal ObservableCollection<OrderInProduct> Products => _products;

        internal OrderInViewModel(OrderIn model)
        {
            _model = model;
            UpdateProducts();

            Commands.Add(new RelayCommand(p =>
            {
                _model.Products.Clear();
                _products.ToList().ForEach(p => _model.Products.Add(new OrderIn.OrderInProduct() { ProductId = p.Product.Id, Count = p.Count }));
                _repository.Update(_model);
            })
            { Name = "Save" });

            _repository.EntityUpdated += ModelUpdated;
        }

        private void ModelUpdated(object? sender, EntityChangedEventArgs e)
        {
            AppHost.GetService<IUIService>().InvokeUIThread(() =>
            {
                _model = (OrderIn)e.Entity;
                OnPropertyChanged(nameof(Model));
                OnPropertyChanged(nameof(Title));
                UpdateProducts();
            });
        }

        private void UpdateProducts()
        {
            _products.Clear();
            _model.Products.ForEach((p) => _products.Add(new OrderInProduct() { Product = _productsRepository.GetById(p.ProductId) as Product ?? throw new ArgumentException(), Count = p.Count }));
        }

        internal class OrderInProduct
        {
            internal Product Product { get; set; }
            internal double Count { get; set; }
        }
    }
}
