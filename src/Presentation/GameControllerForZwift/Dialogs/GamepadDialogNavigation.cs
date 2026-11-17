using GameControllerForZwift.Core;
using GameControllerForZwift.Core.Controller;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;

namespace GameControllerForZwift.UI.WPF.Dialogs
{
    /// <summary>
    /// What a game controller button does in a dialog (CFG-038).
    /// </summary>
    public enum DialogNavigationAction
    {
        None,
        Next,
        Previous,
        Activate,
        Cancel,
    }

    /// <summary>
    /// Turns controller presses into dialog actions: D-pad down or right moves to the next control, up or left to the
    /// previous one, A activates the focused button (or the default button from a text box) and B cancels (CFG-038).
    /// Only new presses count, and the first reading is a baseline, so a button held while the dialog opens does nothing.
    /// </summary>
    public sealed class DialogNavigationTranslator
    {
        private ControllerData? _previous;

        public DialogNavigationAction Translate(ControllerData data)
        {
            ArgumentNullException.ThrowIfNull(data);
            var previous = _previous;
            _previous = data;
            if (previous is null)
                return DialogNavigationAction.None;

            if (data.B && !previous.B)
                return DialogNavigationAction.Cancel;
            if (data.A && !previous.A)
                return DialogNavigationAction.Activate;
            if ((data.DPadDown && !previous.DPadDown) || (data.DPadRight && !previous.DPadRight))
                return DialogNavigationAction.Next;
            if ((data.DPadUp && !previous.DPadUp) || (data.DPadLeft && !previous.DPadLeft))
                return DialogNavigationAction.Previous;
            return DialogNavigationAction.None;
        }
    }

    /// <summary>
    /// Lets the selected game controller drive a dialog window while it is open (CFG-038).
    /// </summary>
    internal sealed class GamepadDialogNavigator : IDisposable
    {
        private readonly IDataIntegrator _dataIntegrator;
        private readonly Window _window;
        private readonly DialogNavigationTranslator _translator = new();
        private readonly Lock _sync = new();

        public GamepadDialogNavigator(IDataIntegrator dataIntegrator, Window window)
        {
            _dataIntegrator = dataIntegrator;
            _window = window;
            _dataIntegrator.InputPolled += DataIntegrator_InputPolled;
        }

        public void Dispose() => _dataIntegrator.InputPolled -= DataIntegrator_InputPolled;

        private void DataIntegrator_InputPolled(object? sender, InputStateChangedEventArgs e)
        {
            DialogNavigationAction action;
            lock (_sync)
                action = _translator.Translate(e.Data);

            if (action != DialogNavigationAction.None)
                _window.Dispatcher.BeginInvoke(() => Apply(action));
        }

        private void Apply(DialogNavigationAction action)
        {
            if (!_window.IsActive && !_window.IsVisible)
                return;

            var focused = Keyboard.FocusedElement as UIElement;
            switch (action)
            {
                case DialogNavigationAction.Next:
                    (focused ?? _window).MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                    break;
                case DialogNavigationAction.Previous:
                    (focused ?? _window).MoveFocus(new TraversalRequest(FocusNavigationDirection.Previous));
                    break;
                case DialogNavigationAction.Activate:
                    var button = focused as Button ?? FindDefaultButton(_window);
                    if (button is { IsEnabled: true } && new ButtonAutomationPeer(button).GetPattern(PatternInterface.Invoke) is IInvokeProvider invoker)
                        invoker.Invoke();
                    break;
                case DialogNavigationAction.Cancel:
                    _window.Close();
                    break;
            }
        }

        private static Button? FindDefaultButton(DependencyObject parent)
        {
            foreach (var child in LogicalTreeHelper.GetChildren(parent).OfType<DependencyObject>())
            {
                if (child is Button { IsDefault: true, IsVisible: true } button)
                    return button;
                if (FindDefaultButton(child) is { } found)
                    return found;
            }
            return null;
        }
    }
}
