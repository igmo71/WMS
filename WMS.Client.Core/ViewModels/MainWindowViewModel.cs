using System.Collections.ObjectModel;
using System.Linq;
using WMS.Client.Core.Infrastructure;
using WMS.Client.Core.Services;

namespace WMS.Client.Core.ViewModels
{
    internal partial class MainWindowViewModel : SafeBindable
    {
        private readonly ObservableCollection<ViewModelBase> _pages = new ObservableCollection<ViewModelBase>();
        private ViewModelBase _currentPage;

        internal ViewModelBase CurrentPage { get => LockAndGet(ref _currentPage); private set => SetAndNotify(ref _currentPage, value); }
        internal ObservableCollection<ViewModelBase> Pages => _pages;

        public MainWindowViewModel()
        {
            CurrentPage = NavigationService.Current;
            UpdatePages();

            NavigationService.CurrentChanged += UpdateCurrent;
            NavigationService.PagesChanged += UpdatePages;
        }

        private void UpdateCurrent(ViewModelBase vm) => CurrentPage = vm;

        private void UpdatePages()
        {
            _pages.Where(vm => !NavigationService.Pages.Contains(vm)).ToList().ForEach(vm => _pages.Remove(vm));
            NavigationService.Pages.Where(vm => !_pages.Contains(vm)).ToList().ForEach(_pages.Add);
        }
    }
}
