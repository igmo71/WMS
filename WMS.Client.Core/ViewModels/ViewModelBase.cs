using WMS.Client.Core.Infrastructure;
using WMS.Client.Core.Services;

namespace WMS.Client.Core.ViewModels
{
    internal abstract class ViewModelBase : SafeBindable
    {
        protected bool _persistent = false;

        internal string Name { get; set; }
        internal string UniqueKey { get; set; }
        internal bool Persistent => _persistent;

        internal RelayCommand CloseCommand { get; }
        internal RelayCommand CurrentCommand { get; }

        protected ViewModelBase()
        {
            CloseCommand = new RelayCommand((p) => NavigationService.ClosePage(this));
            CurrentCommand = new RelayCommand((p) => NavigationService.SetCurrent(this));
        }

    }
}
