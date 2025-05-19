using System;
using System.Collections.ObjectModel;
using System.Linq;
using WMS.Client.Core.Infrastructure;
using WMS.Client.Core.Interfaces;
using WMS.Client.Core.Services;
using WMS.Shared.Models.Documents;

namespace WMS.Client.Core.ViewModels.Documents
{
    internal class DocumentListViewModel : ViewModelBase
    {
        private readonly string _title;
        private readonly IEntityDescriptor _descriptor;
        private readonly ObservableCollection<Document> _documents = new ObservableCollection<Document>();

        internal override string Title => _title;
        internal ObservableCollection<Document> Documents { get => _documents; }

        public RelayCommand OpenCommand { get; }

        public DocumentListViewModel(string name, IEntityDescriptor descriptor)
        {
            _title = name;
            _descriptor = descriptor;
            _descriptor.Repository.EntityUpdated += OnEntityUpdated;

            GetDocuments();

            OpenCommand = new RelayCommand((p) =>
            {
                if (p is Document header)
                {
                    ViewModelDescriptor vmDescriptor = _descriptor.GetMain(_descriptor.Repository.GetById(header.Id));
                    AppHost.GetService<NavigationService>().AddPage(vmDescriptor.UniqueKey, vmDescriptor.Factory);
                }
            });
        }

        private void OnEntityUpdated(object? sender, EntityUpdatedEventArgs e)
        {
            if (_descriptor.Repository.Type == e.Entity.GetType())
                GetDocuments();
        }

        private void GetDocuments()
        {
            _documents.Clear();
            _descriptor.Repository.GetList().OfType<Document>().ToList().ForEach(_documents.Add);
        }
    }
}
