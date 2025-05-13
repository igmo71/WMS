using System;
using WMS.Client.Core.Descriptors;
using WMS.Client.Core.Infrastructure;
using WMS.Client.Core.Interfaces;
using WMS.Client.Core.Services;
using WMS.Shared.Models.Catalogs;

namespace WMS.Client.Core.ViewModels.Catalogs
{
    internal class ProductViewModel : ViewModelBase
    {
        private Product _model;
        private ICatalogDescriptor _descriptor = EntityDescriptorFactory.GetCatalog<Product>();

        internal override string Title => _model.Id != Guid.Empty ? $"Product: {_model.Name}" : $"Product: New";
        internal Product Model { get => LockAndGet(ref _model); private set => SetAndNotify(ref _model, value); }

        public ProductViewModel(Product model)
        {
            _model = model;

            Commands.Add(new RelayCommand(p =>
            {
                if (Model.Id == Guid.Empty)
                {
                    Model = _descriptor.Repository.Create(_model) as Product ?? throw new InvalidCastException();
                    NavigationService.UpdateUniqueKey(_descriptor.GetUniqueKey(Model), this);
                }
                else
                {
                    _descriptor.Repository.Update(_model);
                    OnPropertyChanged(nameof(Model));
                }

                OnPropertyChanged(nameof(Title));
            })
            { Name = "Save" });
        }
    }
}
