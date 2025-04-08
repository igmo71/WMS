using WMS.Client.Core.Infrastructure;
using WMS.Client.Core.Services;
using WMS.Shared.Models.Documents;

namespace WMS.Client.Core.ViewModels
{
    internal class HomeViewModel : PageViewModelBase
    {
        internal RelayCommand OrderInCommand { get; }

        public HomeViewModel()
        {
            Name = "Home";
            _persistent = true;

            OrderInCommand = new RelayCommand((p) => 
            {
                ViewModelDescriptor descriptor = ViewModelResolver.GetList(typeof(OrderIn));
                NavigationService.AddPage(descriptor.UniqueKey, descriptor.Factory);
            });
        }
    }
}
