using WMS.Client.Core.Infrastructure;
using WMS.Client.Core.Services;

namespace WMS.Client.Core.ViewModels
{
    internal class PageViewModelBase : ViewModelBase
    {
        protected bool _persistent = false;
        protected bool _isCurrent = false;

        internal bool Persistent => LockAndGet(ref _persistent);
        internal bool IsCurrent { get => LockAndGet(ref _isCurrent); set => SetAndNotify(ref _isCurrent, value); }

        internal RelayCommand CloseCommand { get; }
        internal RelayCommand CurrentCommand { get; }

        protected PageViewModelBase()
        {
            CloseCommand = new RelayCommand((p) => NavigationService.ClosePage(this), (p) => !_persistent);
            CurrentCommand = new RelayCommand((p) => NavigationService.SetCurrent(this));
            NavigationService.CurrentChanged += (vm) => IsCurrent = vm == this;
        }
    }
}
