using System;
using System.Collections.Generic;
using System.Linq;
using WMS.Client.Core.ViewModels;

namespace WMS.Client.Core.Services
{
    internal static class NavigationService
    {
        private readonly static Dictionary<string, ViewModelBase> _pages = new Dictionary<string, ViewModelBase>();
        private static ViewModelBase _current;

        internal static List<ViewModelBase> Pages => _pages.Select(kvp => kvp.Value).ToList();
        internal static ViewModelBase Current => _current;

        internal static event Action<ViewModelBase> CurrentChanged;
        internal static event Action PagesChanged;

        static NavigationService()
        {
            AddPage(nameof(HomeViewModel), () => new HomeViewModel());
        }

        internal static void AddPage(string uniqueKey, Func<ViewModelBase> factory, bool setCurrent = true)
        {
            ViewModelBase vm = _pages.GetValueOrDefault(uniqueKey);
            if (vm == null)
            {
                vm = factory.Invoke();
                _pages.Add(uniqueKey, vm);
                PagesChanged?.Invoke();
            }

            if (setCurrent)
                SetCurrent(vm);
        }

        internal static void SetCurrent(ViewModelBase vm)
        {
            if (_pages.Where(kvp => kvp.Value == vm).Any())
            {
                _current = vm;
                CurrentChanged?.Invoke(vm);
            }
        }

        internal static void ClosePage(ViewModelBase vm)
        {
            if (vm.Persistent)
                return;

            List<string> keys = _pages.Where(kvp => kvp.Value == vm).Select(kvp => kvp.Key).ToList();
            if (keys.Any())
            {
                keys.ForEach(k => _pages.Remove(k));
                PagesChanged?.Invoke();

                if (_pages.Any())
                    SetCurrent(_pages.LastOrDefault().Value);
            }
        }

        internal static string GetUniqueKey<T>(string postfix = null) where T : ViewModelBase
        {
            return nameof(T) + postfix != null ? "_" + postfix : "";
        }

    }
}
