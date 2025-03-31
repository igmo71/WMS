using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System.Windows.Input;

namespace WMS.Client.Core.Behaviours
{
    internal class ClickBehaviour : AvaloniaObject
    {
        public static readonly AttachedProperty<ICommand> CommandProperty = AvaloniaProperty.RegisterAttached<ClickBehaviour, Control, ICommand>("Command");
        public static readonly AttachedProperty<object> CommandParameterProperty = AvaloniaProperty.RegisterAttached<ClickBehaviour, Control, object>("CommandParameter");

        public static void SetCommand(AvaloniaObject element, ICommand value) => element.SetValue(CommandProperty, value);
        public static ICommand GetCommand(AvaloniaObject element) => element.GetValue(CommandProperty);
        public static void SetCommandParameter(AvaloniaObject element, object value) => element.SetValue(CommandParameterProperty, value);
        public static object GetCommandParameter(AvaloniaObject element) => element.GetValue(CommandParameterProperty);

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
                object parameter = control.GetValue(CommandParameterProperty);

                if (command?.CanExecute(parameter) == true)
                    command.Execute(parameter);
            }
        }
    }
}
