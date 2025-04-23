using System;
using System.Collections.Generic;
using System.Linq;
using WMS.Client.Core.ViewModels;

namespace WMS.Client.Core.Services
{
    internal static class NavigationService
    {
        private static readonly object _lock = new object();
        private static readonly Dictionary<string, ViewModelBase> _pages = new Dictionary<string, ViewModelBase>();
        private static ViewModelBase _current;

        internal static ViewModelBase[] Pages
        {
            get
            {
                lock (_lock)
                    return _pages.Select(kvp => kvp.Value).ToArray();
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

        internal static event EventHandler<CurrentChangedEventArgs> CurrentChanged;
        internal static event EventHandler PagesChanged;

        static NavigationService() => AddPage(nameof(HomeViewModel), () => new HomeViewModel());

        internal static ViewModelBase AddPage(string uniqueKey, Func<ViewModelBase> factory, bool setCurrent = true)
        {
            ViewModelBase vm;
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
                PagesChanged?.Invoke(null, EventArgs.Empty);

            if (setCurrent)
                SetCurrent(vm);

            return vm;
        }

        internal static void SetCurrent(ViewModelBase vm)
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
                CurrentChanged?.Invoke(null, new CurrentChangedEventArgs(vm));
        }

        internal static void ClosePage(ViewModelBase vm)
        {
            if (vm.Persistent)
                return;

            bool invokeEvent = false;

            lock (_lock)
            {
                string key = _pages.FirstOrDefault(kvp => kvp.Value == vm).Key;
                if (key != null)
                {
                    invokeEvent = true;
                    _pages.Remove(key);
                }
            }

            if (invokeEvent)
                PagesChanged?.Invoke(null, EventArgs.Empty);
        }

        internal static void UpdateUniqueKey(string newKey, ViewModelBase vm)
        {
            lock (_lock)
            {
                string oldKey = _pages.FirstOrDefault(kvp => kvp.Value == vm).Key;
                if (oldKey != null)
                {
                    _pages.Remove(oldKey);
                    _pages.Add(newKey, vm);
                }
            }
        }
    }

    internal class CurrentChangedEventArgs : EventArgs
    {
        internal ViewModelBase ViewModel { get; }

        internal CurrentChangedEventArgs(ViewModelBase vm) => ViewModel = vm;
    }
}
