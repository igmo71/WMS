using System.Collections.ObjectModel;
using WMS.Client.Core.Infrastructure;
using WMS.Client.Core.Services;

namespace WMS.Client.Core.ViewModels
{
    internal abstract class ViewModelBase : SafeBindable
    {
        internal virtual string Title => "Unknown";
        internal virtual bool Persistent => false;

        internal RelayCommand CloseCommand { get; }
        internal ObservableCollection<RelayCommand> Commands { get; } = new();

        protected ViewModelBase() => CloseCommand = new RelayCommand((p) => AppHost.GetService<NavigationService>().ClosePage(this), (p) => !Persistent);
    }
}
