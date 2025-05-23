using System;
using System.Collections.Generic;
using System.Linq;
using WMS.Client.Core.ViewModels;

namespace WMS.Client.Core.Services
{
    internal class NavigationService
    {
        private readonly object _lock = new object();
        private readonly List<KeyValuePair<string, ViewModelBase>> _pages = new();
        private ViewModelBase _current;

        internal ViewModelBase[] Pages
        {
            get
            {
                lock (_lock)
                    return _pages.Select(kvp => kvp.Value).ToArray();
            }
        }

        internal ViewModelBase Current
        {
            get
            {
                lock (_lock)
                    return _current;
            }
        }

        internal event EventHandler<CurrentChangedEventArgs> CurrentChanged;
        internal event EventHandler PagesChanged;

        internal ViewModelBase AddPage(string uniqueKey, Func<ViewModelBase> factory, bool setCurrent = true)
        {
            ViewModelBase vm;
            bool invokeEvent = false;

            lock (_lock)
            {
                vm = _pages.FirstOrDefault(kvp => kvp.Key == uniqueKey).Value;
                if (vm == null)
                {
                    vm = factory.Invoke();
                    vm.OnCreate();

                    _pages.Add(new KeyValuePair<string, ViewModelBase>(uniqueKey, vm));
                    invokeEvent = true;
                }
            }

            if (invokeEvent)
                PagesChanged?.Invoke(null, EventArgs.Empty);

            if (setCurrent)
                SetCurrent(vm);

            return vm;
        }

        internal void SetCurrent(ViewModelBase vm)
        {
            bool invokeEvent = false;

            lock (_lock)
            {
                if (_pages.Where(kvp => kvp.Value == vm).Any())
                {
                    if (_current is not null)
                        _current.OnDeactivate();

                    _current = vm;
                    _current.OnActivate();

                    invokeEvent = true;
                }
            }

            if (invokeEvent)
                CurrentChanged?.Invoke(null, new CurrentChangedEventArgs(vm));
        }

        internal void ClosePage(ViewModelBase vm)
        {
            if (vm.Persistent)
                return;

            bool invokeEvent = false;
            ViewModelBase newCurrent = null;

            lock (_lock)
            {
                int index = _pages.FindIndex(kvp => kvp.Value == vm);
                if (index >= 0)
                {
                    _pages.RemoveAt(index);
                    vm.OnClose();

                    if (vm == _current)
                        SetCurrent(index > _pages.Count - 1 ? _pages.Last().Value : _pages[index].Value);

                    invokeEvent = true;
                }
            }

            if (invokeEvent)
                PagesChanged?.Invoke(null, EventArgs.Empty);
        }

        internal void UpdateUniqueKey(string newKey, ViewModelBase vm)
        {
            lock (_lock)
            {
                int index = _pages.FindIndex(kvp => kvp.Value == vm);
                if (index > 0)
                {
                    _pages.RemoveAt(index);
                    _pages.Insert(index, new KeyValuePair<string, ViewModelBase>(newKey, vm));
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
