using WMS.Shared.Models.Catalogs;

namespace WMS.Client.Core.ViewModels
{
    internal class ProductViewModel : ViewModelBase
    {
        private Product _model;

        internal override string Name => $"Product: {_model.Name}";
        internal Product Model => _model;

        public ProductViewModel(Product model)
        {
            _model = model;
        }
    }
}
