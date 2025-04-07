using WMS.Client.Core.Infrastructure;
using WMS.Client.Core.Services;

namespace WMS.Client.Core.ViewModels
{
    internal class HomeViewModel : PageViewModelBase
    {
        internal RelayCommand OrderInCommand { get; }

        public HomeViewModel()
        {
            Name = "Home";
            _persistent = true;

            OrderInCommand = new RelayCommand((p) => NavigationService.AddPage(nameof(DocumentListViewModel), () => new DocumentListViewModel() { Name = "Order In" } ));
        }
    }
}
