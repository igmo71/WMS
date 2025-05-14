using System;
using System.Collections.ObjectModel;
using System.Linq;
using WMS.Client.Core.Infrastructure;
using WMS.Client.Core.Interfaces;
using WMS.Client.Core.Services;
using WMS.Shared.Models;
using WMS.Shared.Models.Catalogs;

namespace WMS.Client.Core.ViewModels.Catalogs
{
    internal class CatalogListViewModel : ViewModelBase
    {
        private readonly string _title;
        private readonly ICatalogDescriptor _descriptor;
        private readonly ObservableCollection<Catalog> _catalog = new ObservableCollection<Catalog>();

        internal override string Title => _title;
        internal ObservableCollection<Catalog> Catalog => _catalog;

        public RelayCommand OpenCommand { get; }

        public CatalogListViewModel(string name, ICatalogDescriptor descriptor)
        {
            _title = name;
            _descriptor = descriptor;

            _descriptor.Repository.EntityCreated += OnEntityCreated;
            _descriptor.Repository.EntityUpdated += OnEntityUpdated;

            GetProducts();

            OpenCommand = new RelayCommand((p) =>
            {
                if (p is EntityBase header)
                {
                    ViewModelDescriptor vmDescriptor = _descriptor.GetMain(_descriptor.Repository.GetById(header.Id) as Catalog ?? throw new InvalidCastException());
                    AppHost.GetService<NavigationService>().AddPage(vmDescriptor.UniqueKey, vmDescriptor.Factory);
                }
            });

            Commands.Add(new RelayCommand((p) =>
            {
                ViewModelDescriptor vmDescriptor = _descriptor.GetMain(_descriptor.CreateNew());
                AppHost.GetService<NavigationService>().AddPage(vmDescriptor.UniqueKey, vmDescriptor.Factory);
            })
            { Name = "Create" });
        }

        private void OnEntityUpdated(object? sender, EntityChangedEventArgs e) => GetProducts();

        private void OnEntityCreated(object? sender, EntityChangedEventArgs e) => GetProducts();

        private void GetProducts()
        {
            _catalog.Clear();
            _descriptor.Repository.GetList().OfType<Catalog>().ToList().ForEach(_catalog.Add);
        }
    }
}
