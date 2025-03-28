using WMS.Client.Core.Infrastructure;
using WMS.Client.Core.Services;

namespace WMS.Client.Core.ViewModels
{
    internal class HomeViewModel : ViewModelBase
    {
        internal RelayCommand DocumentsCommand { get; }

        public HomeViewModel(string uniqueKey) : base(uniqueKey)
        {
            Name = "Home";
            _persistent = true;

            DocumentsCommand = new RelayCommand((p) => NavigationService.AddPage(nameof(DocumentListViewModel), () => new DocumentListViewModel(nameof(DocumentListViewModel))));
        }
    }
}
