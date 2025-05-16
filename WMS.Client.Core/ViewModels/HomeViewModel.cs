using WMS.Client.Core.Descriptors;
using WMS.Client.Core.Infrastructure;
using WMS.Client.Core.Services;
using WMS.Shared.Models.Catalogs;
using WMS.Shared.Models.Documents;

namespace WMS.Client.Core.ViewModels
{
    internal class HomeViewModel : ViewModelBase
    {
        internal override string Title => "Home";
        internal override bool Persistent => true;
        internal RelayCommand OrderInCommand { get; }
        internal RelayCommand OrderOutCommand { get; }
        internal RelayCommand ProductsCommand { get; }

        public HomeViewModel()
        {
            OrderInCommand = new RelayCommand((p) => 
            {
                ViewModelDescriptor descriptor = EntityDescriptorFactory.Get<OrderIn>().GetList();
                AppHost.GetService<NavigationService>().AddPage(descriptor.UniqueKey, descriptor.Factory);
            });

            OrderOutCommand = new RelayCommand((p) => 
            {
                ViewModelDescriptor descriptor = EntityDescriptorFactory.Get<OrderOut>().GetList();
                AppHost.GetService<NavigationService>().AddPage(descriptor.UniqueKey, descriptor.Factory);
            });

            ProductsCommand = new RelayCommand((p) => 
            {
                ViewModelDescriptor descriptor = EntityDescriptorFactory.Get<Product>().GetList();
                AppHost.GetService<NavigationService>().AddPage(descriptor.UniqueKey, descriptor.Factory);
            });
        }
    }
}
