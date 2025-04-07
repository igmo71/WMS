using System.Collections.ObjectModel;
using WMS.Client.Core.Infrastructure;
using WMS.Client.Core.Services;
using WMS.Shared.Models.Documents;

namespace WMS.Client.Core.ViewModels
{
    internal class DocumentListViewModel : PageViewModelBase
    {
        private readonly ObservableCollection<Document> _documents = new ObservableCollection<Document>();

        internal ObservableCollection<Document> Documents { get => _documents; }

        public RelayCommand OpenCommand { get; }

        public DocumentListViewModel()
        {
            Name = "Documents";
            GetDocuments();

            OpenCommand = new RelayCommand((p) =>
            {
                if (p is OrderIn item)
                {
                    OrderIn orderIn = HTTPService.GetObject<OrderIn>(item.Id.ToString());
                    if (orderIn != null)
                        NavigationService.AddPage($"{nameof(OrderIn)}{orderIn.Id}", () => new OrderInViewModel(orderIn));
                }
            });
        }

        private void GetDocuments()
        {
            _documents.Clear();
            HTTPService.GetList<OrderIn>().ForEach(_documents.Add);
        }
    }
}
