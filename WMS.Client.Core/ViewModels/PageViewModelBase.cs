using WMS.Client.Core.Infrastructure;
using WMS.Client.Core.Services;

namespace WMS.Client.Core.ViewModels
{
    internal class PageViewModelBase : ViewModelBase
    {
        protected bool _persistent = false;

        internal bool Persistent => LockAndGet(ref _persistent);

        internal RelayCommand CloseCommand { get; }

        protected PageViewModelBase()
        {
            CloseCommand = new RelayCommand((p) => NavigationService.ClosePage(this), (p) => !_persistent);
        }
    }
}
