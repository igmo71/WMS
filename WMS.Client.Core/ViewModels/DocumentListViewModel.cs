using System.Collections.ObjectModel;
using System.Linq;
using WMS.Client.Core.Infrastructure;
using WMS.Client.Core.Interfaces;
using WMS.Client.Core.Services;
using WMS.Shared.Models;
using WMS.Shared.Models.Documents;

namespace WMS.Client.Core.ViewModels
{
    internal class DocumentListViewModel : PageViewModelBase
    {
        private readonly IEntityRepository _repository;
        private readonly ObservableCollection<Document> _documents = new ObservableCollection<Document>();

        internal ObservableCollection<Document> Documents { get => _documents; }

        public RelayCommand OpenCommand { get; }

        public DocumentListViewModel(IEntityRepository repository)
        {
            Name = "Documents";
            _repository = repository;

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

        private void GetDocuments()
        {
            _documents.Clear();
            _repository.GetList().OfType<Document>().ToList().ForEach(_documents.Add);
        }
    }
}
