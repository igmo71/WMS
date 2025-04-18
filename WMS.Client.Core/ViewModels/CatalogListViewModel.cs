using System.Collections.ObjectModel;
using System.Linq;
using WMS.Client.Core.Infrastructure;
using WMS.Client.Core.Interfaces;
using WMS.Client.Core.Services;
using WMS.Shared.Models;
using WMS.Shared.Models.Catalogs;

namespace WMS.Client.Core.ViewModels
{
    internal class CatalogListViewModel : ViewModelBase
    {
        private readonly string _name;
        private readonly IEntityRepository _repository;
        private readonly ObservableCollection<Catalog> _catalog = new ObservableCollection<Catalog>();

        internal override string Name => _name;
        internal ObservableCollection<Catalog> Catalog => _catalog;

        public RelayCommand OpenCommand { get; }

        public CatalogListViewModel(string name, IEntityRepository repository)
        {
            _name = name;
            _repository = repository;

            GetProducts();

            OpenCommand = new RelayCommand((p) =>
            {
                if (p is EntityBase header)
                {
                    EntityBase entity = _repository.GetById(header.Id);
                    if (entity != null)
                    {
                        ViewModelDescriptor descriptor = ViewModelResolver.GetMain(entity);
                        NavigationService.AddPage(descriptor.UniqueKey, descriptor.Factory);
                    }
                }
            });
        }

        private void GetProducts()
        {
            _catalog.Clear();
            _repository.GetList().OfType<Catalog>().ToList().ForEach(_catalog.Add);
        }
    }
}
