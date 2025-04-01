using WMS.Client.Core.Infrastructure;
using WMS.Client.Core.Services;

namespace WMS.Client.Core.ViewModels
{
    internal abstract class ViewModelBase : SafeBindable
    {
        protected bool _persistent = false;
        protected bool _isCurrent = false;

        internal string Name { get; set; }
        internal bool Persistent => _persistent;
        internal bool IsCurrent { get => LockAndGet(ref _isCurrent); set => SetAndNotify(ref _isCurrent, value); }

        internal RelayCommand CloseCommand { get; }
        internal RelayCommand CurrentCommand { get; }

        protected ViewModelBase()
        {
            CloseCommand = new RelayCommand((p) => NavigationService.ClosePage(this));
            CurrentCommand = new RelayCommand((p) => NavigationService.SetCurrent(this));
            NavigationService.CurrentChanged += (vm) => IsCurrent = NavigationService.Current == this;
        }

    }
}
