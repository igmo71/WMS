using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace WMS.Client.Core.Behaviours
{
    internal class TappedBehaviour : AvaloniaObject
    {
        public static readonly AttachedProperty<ICommand> CommandProperty = AvaloniaProperty.RegisterAttached<TappedBehaviour, Interactive, ICommand>("Command");
        public static readonly AttachedProperty<object> CommandParameterProperty = AvaloniaProperty.RegisterAttached<TappedBehaviour, Interactive, object>("CommandParameter");

        public static void SetCommand(AvaloniaObject element, ICommand value) => element.SetValue(CommandProperty, value);
        public static ICommand GetCommand(AvaloniaObject element) => element.GetValue(CommandProperty);
        public static void SetCommandParameter(AvaloniaObject element, object value) => element.SetValue(CommandParameterProperty, value);
        public static object GetCommandParameter(AvaloniaObject element) => element.GetValue(CommandParameterProperty);

        static TappedBehaviour() => CommandProperty.Changed.AddClassHandler<Interactive>(PropertyChangedHandler);

        static readonly HashSet<Interactive> _elements = new HashSet<Interactive>();

        public static void PropertyChangedHandler(Interactive element, AvaloniaPropertyChangedEventArgs args)
        {
            if (args.OldValue is ICommand oldCommand)
                oldCommand.CanExecuteChanged -= CanExecuteChanged;

            if (args.NewValue is ICommand newCommand)
            {
                _elements.Add(element);
                element.AddHandler(InputElement.TappedEvent, EventHandler);
                UpdateIsEnabled(element, newCommand);
                newCommand.CanExecuteChanged += CanExecuteChanged;
            }
            else
            {
                _elements.Remove(element);
                element.RemoveHandler(InputElement.TappedEvent, EventHandler);
            }
        }

        private static void CanExecuteChanged(object? sender, EventArgs e)
        {
            if (sender is ICommand command)
                _elements.Where((e) => GetCommand(e) == command).ToList().ForEach((e) => UpdateIsEnabled(e, command));
        }

        private static void UpdateIsEnabled(Interactive element, ICommand command)
        {
            if (element is Control control)
                control.IsEnabled = command?.CanExecute(GetCommandParameter(element)) ?? true;
        }

        public static void EventHandler(object sender, RoutedEventArgs args)
        {
            if (sender is Interactive interactive)
            {
                ICommand command = interactive.GetValue(CommandProperty);
                object parameter = interactive.GetValue(CommandParameterProperty);

                if (command?.CanExecute(parameter) == true)
                    command.Execute(parameter);
            }
        }
    }
}
