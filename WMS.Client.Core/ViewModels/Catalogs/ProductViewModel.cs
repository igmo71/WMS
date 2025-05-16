using System;
using WMS.Client.Core.Adapters.Catalogs;
using WMS.Client.Core.Descriptors;
using WMS.Client.Core.Infrastructure;
using WMS.Client.Core.Interfaces;
using WMS.Client.Core.Services;
using WMS.Shared.Models.Catalogs;

namespace WMS.Client.Core.ViewModels.Catalogs
{
    internal class ProductViewModel : ViewModelBase
    {
        private readonly ProductAdapter _adapter;
        private readonly IEntityDescriptor _descriptor = EntityDescriptorFactory.Get<Product>();

        internal override string Title => _adapter.Id != Guid.Empty ? $"Product: {_adapter.Name}" : $"Product: New";
        internal ProductAdapter Adapter => _adapter;

        public ProductViewModel(ProductAdapter adapter)
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
    }
}
