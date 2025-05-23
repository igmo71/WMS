using System;
using System.Windows.Input;

namespace WMS.Client.Core.Infrastructure
{
    internal class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        public event EventHandler? CanExecuteChanged;

        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public void Execute(object? parameter) => _execute?.Invoke(parameter);

        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

        public void OnCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
