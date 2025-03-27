using WMS.Client.Core.Infrastructure;
using WMS.Client.Core.Services;

namespace WMS.Client.Core.ViewModels
{
    internal abstract class ViewModelBase : SafeBindable
    {
        protected bool _persistent = false;

        internal string Name { get; set; }
        internal bool Persistent => _persistent;

        internal RelayCommand CloseCommand { get; }

        public ViewModelBase() 
        {
            CloseCommand = new RelayCommand((p) => Close());
        }

        internal virtual void Close() => NavigationService.ClosePage(this);
    }
}
