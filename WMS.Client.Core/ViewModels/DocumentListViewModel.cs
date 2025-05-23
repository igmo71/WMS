using System;
using System.Collections.ObjectModel;
using System.Linq;
using WMS.Client.Core.Infrastructure;
using WMS.Client.Core.Interfaces;
using WMS.Client.Core.Services;
using WMS.Shared.Models;
using WMS.Shared.Models.Documents;

namespace WMS.Client.Core.ViewModels
{
    internal class DocumentListViewModel : ViewModelBase
    {
        private readonly string _name;
        private readonly IEntityRepository _repository;
        private readonly ObservableCollection<Document> _documents = new ObservableCollection<Document>();

        internal override string Name => _name;
        internal ObservableCollection<Document> Documents { get => _documents; }

        public RelayCommand OpenCommand { get; }

        public DocumentListViewModel(string name, IEntityRepository repository)
        {
            _name = name;
            _repository = repository;
            _repository.EntityUpdated += OnEntityUpdated;

            GetDocuments();

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

        private void OnEntityUpdated(object? sender, EntityChangedEventArgs e)
        {
            if (_repository.Type == e.Entity.GetType()) 
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
            _repository.GetList().OfType<Document>().ToList().ForEach(_documents.Add);
        }
    }
}
