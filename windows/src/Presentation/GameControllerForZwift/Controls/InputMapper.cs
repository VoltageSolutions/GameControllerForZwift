using GameControllerForZwift.Core;
using System.Windows;
using System.Windows.Controls;

namespace GameControllerForZwift.UI.WPF.Controls
{
    public class InputMapper : UserControl
    {
        public string InputName
        {
            get { return (string)GetValue(InputNameProperty); }
            set { SetValue(InputNameProperty, value); }
        }
        public static readonly DependencyProperty InputNameProperty =
            DependencyProperty.Register("InputName", typeof(string), typeof(InputMapper), new PropertyMetadata(""));

        public bool InputPressed
        {
            get { return (bool)GetValue(InputPressedProperty); }
            set { SetValue(InputPressedProperty, value); }
        }
        public static readonly DependencyProperty InputPressedProperty =
            DependencyProperty.Register("InputPressed", typeof(bool), typeof(InputMapper), new PropertyMetadata(false));

        // Dependency property for ZwiftFunction selection
        public ZwiftFunction SelectedFunction
        {
            get { return (ZwiftFunction)GetValue(SelectedFunctionProperty); }
            set { SetValue(SelectedFunctionProperty, value); }
        }
        public static readonly DependencyProperty SelectedFunctionProperty =
            DependencyProperty.Register(nameof(SelectedFunction), typeof(ZwiftFunction), typeof(InputMapper),
                new PropertyMetadata(ZwiftFunction.None));
    }
}
