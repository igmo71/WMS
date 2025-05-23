using Avalonia;
using Avalonia.Input;
using Avalonia.Interactivity;
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

        public static void PropertyChangedHandler(Interactive element, AvaloniaPropertyChangedEventArgs args)
        {
            if (args.NewValue is ICommand newCommand)
            {
                element.AddHandler(InputElement.TappedEvent, EventHandler);
            }
            else
            {
                element.RemoveHandler(InputElement.TappedEvent, EventHandler);
            }
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
