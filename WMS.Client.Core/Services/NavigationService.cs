using System;
using System.Collections.Generic;
using System.Linq;
using WMS.Client.Core.ViewModels;

namespace WMS.Client.Core.Services
{
    internal static class NavigationService
    {
        private static readonly object _lock = new object();
        private static readonly Dictionary<string, PageViewModelBase> _pages = new Dictionary<string, PageViewModelBase>();
        private static PageViewModelBase _current;

        internal static List<PageViewModelBase> Pages
        {
            get
            {
                lock (_lock)
                    return _pages.Select(kvp => kvp.Value).ToList();
            }
        }

        internal static ViewModelBase Current
        {
            get
            {
                lock (_lock)
                    return _current;
            }
        }

        internal static event Action<PageViewModelBase> CurrentChanged;
        internal static event Action PagesChanged;

        static NavigationService() => AddPage(GetUniqueKey<HomeViewModel>(), () => new HomeViewModel());

        internal static string GetUniqueKey<T>(string postfix = null) where T : PageViewModelBase => nameof(T) + postfix != null ? "_" + postfix : "";

        internal static PageViewModelBase AddPage(string uniqueKey, Func<PageViewModelBase> factory, bool setCurrent = true)
        {
            PageViewModelBase vm;
            bool invokeEvent = false;

            lock (_lock)
            {
                vm = _pages.GetValueOrDefault(uniqueKey);
                if (vm == null)
                {
                    vm = factory.Invoke();
                    _pages.Add(uniqueKey, vm);
                    invokeEvent = true;
                }
            }

            if (invokeEvent)
                PagesChanged?.Invoke();

            if (setCurrent)
                SetCurrent(vm);

            return vm;
        }

        internal static void SetCurrent(PageViewModelBase vm)
        {
            bool invokeEvent = false;

            lock (_lock)
            {
                if (_pages.Where(kvp => kvp.Value == vm).Any())
                {
                    _current = vm;
                    invokeEvent = true;
                }
            }

            if (invokeEvent)
                CurrentChanged?.Invoke(vm);
        }

        internal static void ClosePage(PageViewModelBase vm)
        {
            if (vm.Persistent)
                return;

            bool invokeEvent = false;

            lock (_lock)
            {
                List<string> keys = _pages.Where(kvp => kvp.Value == vm).Select(kvp => kvp.Key).ToList();
                if (keys.Any())
                {
                    keys.ForEach(k => _pages.Remove(k));
                    invokeEvent = true;
                    if (_pages.Any())
                        SetCurrent(_pages.LastOrDefault().Value);
                }
            }

            if (invokeEvent)
                PagesChanged?.Invoke();
        }

    }
}
