using System.Collections.ObjectModel;
using WMS.Client.Core.Services;
using WMS.Shared.Models.Catalogs;
using WMS.Shared.Models.Documents;

namespace WMS.Client.Core.ViewModels
{
    internal class OrderInViewModel : PageViewModelBase
    {
        private readonly OrderIn _model;
        private readonly ObservableCollection<Product> _products = new ObservableCollection<Product>();

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
                _products.Add(HTTPService.GetObject<Product>(p.ProductId)));
        }
    }
}
