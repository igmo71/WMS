using WMS.Client.Core.Infrastructure;
using WMS.Client.Core.Services;

namespace WMS.Client.Core.ViewModels
{
    internal class ViewModelBase : SafeBindable
    {
        private string _name = "Unknown";
        protected bool _persistent = false;

        internal string Name { get => LockAndGet(ref _name); set => SetAndNotify(ref _name, value); }
        internal bool Persistent => LockAndGet(ref _persistent);

        internal RelayCommand CloseCommand { get; }

        protected ViewModelBase()
        {
            CloseCommand = new RelayCommand((p) => NavigationService.ClosePage(this), (p) => !_persistent);
        }
    }
}
