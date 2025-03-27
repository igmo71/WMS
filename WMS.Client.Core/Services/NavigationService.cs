using System;
using System.Collections.Generic;
using System.Linq;
using WMS.Client.Core.ViewModels;

namespace WMS.Client.Core.Services
{
    internal static class NavigationService
    {
        private readonly static List<ViewModelBase> _pages = new List<ViewModelBase>();
        private static ViewModelBase _current;

        internal static List<ViewModelBase> Pages => _pages;
        internal static ViewModelBase Current => _current;

        public static event Action<ViewModelBase> CurrentChanged;
        public static event Action PagesChanged;

        static NavigationService()
        {
            _current = new HomeViewModel();
            _pages.Add(_current);
            CurrentChanged?.Invoke(_current);
        }

        internal static void SetCurrent(ViewModelBase vm)
        {
            if (_pages.Any(e => e == vm))
            {
                _current = vm;
                CurrentChanged?.Invoke(vm);
            }
        }

        internal static void AddPage(ViewModelBase vm, bool setCurrent = true)
        {
            if (!_pages.Contains(vm))
            {
                _pages.Add(vm);
                PagesChanged?.Invoke();
            }

            if (setCurrent)
                SetCurrent(vm);
        }

        internal static void ClosePage(ViewModelBase vm)
        {
            if (_pages.Contains(vm))
            {
                _pages.Remove(vm);
                PagesChanged?.Invoke();
                SetCurrent(_pages.LastOrDefault());
            }
        }

    }
}
