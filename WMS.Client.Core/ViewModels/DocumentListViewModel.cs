using System;
using System.Collections.ObjectModel;
using System.Linq;
using WMS.Client.Core.Infrastructure;
using WMS.Client.Core.Interfaces;
using WMS.Client.Core.Services;
using WMS.Shared.Models.Documents;

namespace WMS.Client.Core.ViewModels
{
    internal class DocumentListViewModel : ViewModelBase
    {
        private readonly string _name;
        private readonly IDocumentDescriptor _descriptor;
        private readonly ObservableCollection<Document> _documents = new ObservableCollection<Document>();

        internal override string Name => _name;
        internal ObservableCollection<Document> Documents { get => _documents; }

        public RelayCommand OpenCommand { get; }

        public DocumentListViewModel(string name, IDocumentDescriptor descriptor)
        {
            _name = name;
            _descriptor = descriptor;
            _descriptor.Repository.EntityUpdated += OnEntityUpdated;

            GetDocuments();

            OpenCommand = new RelayCommand((p) =>
            {
                if (p is Document header)
                {
                    ViewModelDescriptor vmDescriptor = _descriptor.GetMain(_descriptor.Repository.GetById(header.Id) as Document ?? throw new InvalidCastException());
                    NavigationService.AddPage(vmDescriptor.UniqueKey, vmDescriptor.Factory);
                }
            });
        }

        private void OnEntityUpdated(object? sender, EntityChangedEventArgs e)
        {
            if (_descriptor.Repository.Type == e.Entity.GetType())
            {
                //int index = _documents.ToList().FindIndex((d) => d.Id == e.Entity.Id);
                //if (index > 0)
                //    _documents[index] = e.Entity as Document ?? throw new InvalidCastException();
                GetDocuments();
            }
        }

        private void GetDocuments()
        {
            _documents.Clear();
            _descriptor.Repository.GetList().OfType<Document>().ToList().ForEach(_documents.Add);
        }
    }
}
