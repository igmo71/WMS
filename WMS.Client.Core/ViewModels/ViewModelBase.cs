using WMS.Client.Core.Infrastructure;
using WMS.Client.Core.Services;

namespace WMS.Client.Core.ViewModels
{
    internal abstract class ViewModelBase : SafeBindable
    {
        internal virtual string Name => "Unknown";
        internal virtual bool Persistent => false;

        internal RelayCommand CloseCommand { get; }

        protected ViewModelBase()
        {
            CloseCommand = new RelayCommand((p) => NavigationService.ClosePage(this), (p) => !Persistent);
        }
    }
}
