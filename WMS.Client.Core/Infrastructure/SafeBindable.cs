using System.Collections.Concurrent;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using WMS.Client.Core.Interfaces;
using WMS.Client.Core.Services;

namespace WMS.Client.Core.Infrastructure
{
    internal abstract class SafeBindable : INotifyPropertyChanged
    {

        private readonly ConcurrentDictionary<string, object> _locks = new ConcurrentDictionary<string, object>();
        protected object GetLock(string key) => _locks.GetOrAdd(key, k => new object());


        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string property = null) => AppHost.GetService<IUIService>().InvokeUIThread(() => PropertyChanged?.Invoke(() => this, new PropertyChangedEventArgs(property)));

        protected T LockAndGet<T>(ref T? value, [CallerMemberName] string property = null)
        {
            lock (GetLock(property))
                return value;
        }

        protected bool SetAndNotify<T>(ref T? field, T? value, [CallerMemberName] string property = null)
        {
            lock (GetLock(property))
            {
                if (Equals(field, value))
                    return false;

                field = value;
            }

            OnPropertyChanged(property);
            return true;
        }

    }
}
