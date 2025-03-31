using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace WMS.Client.Core.Behaviours
{
    internal class ClickBehaviour : AvaloniaObject
    {
        public static readonly AttachedProperty<ICommand> CommandProperty = AvaloniaProperty.RegisterAttached<ClickBehaviour, Control, ICommand>("Command");

        public static void SetCommand(AvaloniaObject element, ICommand value) => element.SetValue(CommandProperty, value);

        public static ICommand GetCommand(AvaloniaObject element) => element.GetValue(CommandProperty);

        static ClickBehaviour() => CommandProperty.Changed.AddClassHandler<Control>(PropertyChangedHandler);

        static void PropertyChangedHandler(Control element, AvaloniaPropertyChangedEventArgs args)
        {
            if (args.NewValue is ICommand commandValue)
                element.AddHandler(InputElement.TappedEvent, EventHandler);
            else
                element.RemoveHandler(InputElement.TappedEvent, EventHandler);
        }
        static void EventHandler(object sender, RoutedEventArgs args)
        {
            if (sender is Control control)
            {
                ICommand command = control.GetValue(CommandProperty);
                if (command?.CanExecute(null) == true)
                    command.Execute(null);
            }
        }
    }
}
