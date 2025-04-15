using WMS.Client.Core.Infrastructure;
using WMS.Client.Core.Services;
using WMS.Shared.Models.Documents;

namespace WMS.Client.Core.ViewModels
{
    internal class HomeViewModel : ViewModelBase
    {
        internal override string Name => "Home";
        internal override bool Persistent => true;
        internal RelayCommand OrderInCommand { get; }
        internal RelayCommand OrderOutCommand { get; }

        public HomeViewModel()
        {
            OrderInCommand = new RelayCommand((p) => 
            {
                ViewModelDescriptor descriptor = ViewModelResolver.GetList(typeof(OrderIn));
                NavigationService.AddPage(descriptor.UniqueKey, descriptor.Factory);
            });

            OrderOutCommand = new RelayCommand((p) => 
            {
                ViewModelDescriptor descriptor = ViewModelResolver.GetList(typeof(OrderOut));
                NavigationService.AddPage(descriptor.UniqueKey, descriptor.Factory);
            });
        }
    }
}
