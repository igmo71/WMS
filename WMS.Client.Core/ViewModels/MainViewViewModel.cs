using System.Collections.ObjectModel;
using System.Linq;
using WMS.Client.Core.Services;
using WMS.Client.Core.Infrastructure;

namespace WMS.Client.Core.ViewModels
{
    internal partial class MainViewModel : ViewModelBase
    {
        private readonly ObservableCollection<ViewModelBase> _pages = new ObservableCollection<ViewModelBase>();
        private PageViewModelBase _currentPage;

        internal PageViewModelBase CurrentPage { get => LockAndGet(ref _currentPage); set => NavigationService.SetCurrent(value); }
        internal ObservableCollection<ViewModelBase> Pages => _pages;

        internal RelayCommand SetCurrentCommand { get; }

        public MainViewModel()
        {
            _currentPage = NavigationService.Current;
            UpdatePages();

            NavigationService.CurrentChanged += UpdateCurrent;
            NavigationService.PagesChanged += UpdatePages;

            SetCurrentCommand = new RelayCommand((p) =>
            {
                if (p is PageViewModelBase page)
                    NavigationService.SetCurrent(page);
            });
        }

        private void UpdateCurrent(PageViewModelBase vm) => SetAndNotify(ref _currentPage, vm, nameof(CurrentPage));

        private void UpdatePages()
        {
            _pages.Where(vm => !NavigationService.Pages.Contains(vm)).ToList().ForEach(vm => _pages.Remove(vm));
            NavigationService.Pages.Where(vm => !_pages.Contains(vm)).ToList().ForEach(_pages.Add);
        }
    }
}
