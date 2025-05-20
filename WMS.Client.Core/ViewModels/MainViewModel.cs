using System;
using System.Collections.ObjectModel;
using System.Linq;
using WMS.Client.Core.Infrastructure;
using WMS.Client.Core.Services;

namespace WMS.Client.Core.ViewModels
{
    internal partial class MainViewModel : SafeBindable
    {
        private readonly ObservableCollection<ViewModelBase> _pages = new();
        private ViewModelBase _currentPage;

        internal ViewModelBase CurrentPage { get => LockAndGet(ref _currentPage); set => AppHost.GetService<NavigationService>().SetCurrent(value); }
        internal ObservableCollection<ViewModelBase> Pages => _pages;

        internal RelayCommand SetCurrentCommand { get; }

        public MainViewModel()
        {
            NavigationService navigator = AppHost.GetService<NavigationService>();
            navigator.CurrentChanged += OnCurrentChanged;
            navigator.PagesChanged += OnPagesChanged;
            navigator.AddPage(nameof(HomeViewModel), () => new HomeViewModel());

            SetCurrentCommand = new RelayCommand((p) =>
            {
                if (p is ViewModelBase page)
                    AppHost.GetService<NavigationService>().SetCurrent(page);
            });
        }

        private void OnCurrentChanged(object? sender, CurrentChangedEventArgs args) => SetAndNotify(ref _currentPage, args.ViewModel, nameof(CurrentPage));

        private void OnPagesChanged(object? sender, EventArgs args) => SyncPages();

        private void SyncPages()
        {
            lock (GetLock(nameof(_pages)))
            {
                ViewModelBase[] navigatorPages = AppHost.GetService<NavigationService>().Pages;
                _pages.Except(navigatorPages).ToList().ForEach(vm => _pages.Remove(vm));
                navigatorPages.Except(_pages).ToList().ForEach(_pages.Add);
            }
        }
    }
}
