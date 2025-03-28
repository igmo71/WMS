using WMS.Client.Core.Infrastructure;
using WMS.Client.Core.Services;

namespace WMS.Client.Core.ViewModels
{
    internal abstract class ViewModelBase : SafeBindable
    {
        protected bool _persistent = false;
        protected readonly string _uniqueKey;

        internal string Name { get; set; }
        internal bool Persistent => _persistent;
        internal string UniqueKey => _uniqueKey;

        internal RelayCommand CloseCommand { get; }
        internal RelayCommand CurrentCommand { get; }

        protected ViewModelBase(string uniqueKey)
        {
            _uniqueKey = uniqueKey;

            CloseCommand = new RelayCommand((p) => NavigationService.ClosePage(this));
            CurrentCommand = new RelayCommand((p) => NavigationService.SetCurrent(this));
        }

    }
}
