using WMS.Client.Core.Adapters.Documents;
using WMS.Client.Core.Descriptors;
using WMS.Client.Core.Infrastructure;
using WMS.Client.Core.Interfaces;
using WMS.Client.Core.Services;
using WMS.Shared.Models.Documents;

namespace WMS.Client.Core.ViewModels.Documents
{
    internal class OrderInViewModel : ViewModelBase
    {
        private OrderInAdapter _adapter;
        private readonly IEntityDescriptor _descriptor = EntityDescriptorFactory.Get<OrderIn>();

        internal override string Title => $"{nameof(OrderIn)} {_adapter.Number} {_adapter.DateTime}";
        internal OrderInAdapter Adapter => _adapter;

        internal OrderInViewModel(OrderInAdapter adapter)
        {
            _adapter = adapter;

            Commands.Add(new RelayCommand(p =>
            {
                _adapter.Save();
                AppHost.GetService<NavigationService>().UpdateUniqueKey(_descriptor.GetUniqueKey(_adapter), this);
                OnPropertyChanged(nameof(Title));
            })
            { Name = "Save" });
        }

        protected override void ProcessBarcode(string barcode) => _adapter.AddProduct(barcode);
    }
}
