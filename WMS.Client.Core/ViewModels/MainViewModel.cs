using System.Collections.ObjectModel;
using System.Linq;
using WMS.Client.Core.Services;
using WMS.Client.Core.Infrastructure;
using System;
using System.Collections.Generic;

namespace WMS.Client.Core.ViewModels
{
    internal partial class MainViewModel : SafeBindable
    {
        private readonly object _lock = new();
        private readonly ObservableCollection<ViewModelBase> _pages = new();
        private ViewModelBase _currentPage;

        internal ViewModelBase CurrentPage { get => LockAndGet(ref _currentPage); set => NavigationService.SetCurrent(value); }
        internal ObservableCollection<ViewModelBase> Pages => _pages;

        internal RelayCommand SetCurrentCommand { get; }

        public MainViewModel()
        {
            _currentPage = NavigationService.Current;
            SyncPages();

            NavigationService.CurrentChanged += OnCurrentChanged;
            NavigationService.PagesChanged += OnPagesChanged;

            SetCurrentCommand = new RelayCommand((p) =>
            {
                if (p is ViewModelBase page)
                    NavigationService.SetCurrent(page);
            });
        }

        private void OnCurrentChanged(object? sender, CurrentChangedEventArgs args) => SetAndNotify(ref _currentPage, args.ViewModel, nameof(CurrentPage));

        private void OnPagesChanged(object? sender, EventArgs args) => SyncPages();

        private void SyncPages()
        {
            lock (_lock)
            {
                ViewModelBase[] navigatorPages = NavigationService.Pages;
                List<ViewModelBase> removePages = _pages.Except(navigatorPages).ToList();
                List<ViewModelBase> addPages = navigatorPages.Except(_pages).ToList();

                if (removePages.Contains(CurrentPage))
                {
                    int currentIndex = _pages.IndexOf(CurrentPage);

                    ViewModelBase newCurrent = _pages.Skip(currentIndex + 1).Where(vm => !removePages.Contains(vm)).FirstOrDefault();
                    if (newCurrent == null)
                        newCurrent = _pages.Take(currentIndex).Where(vm => !removePages.Contains(vm)).LastOrDefault();

                    NavigationService.SetCurrent(newCurrent);
                }

                removePages.ForEach(vm => _pages.Remove(vm));
                addPages.ForEach(_pages.Add);
            }
        }
    }
}
