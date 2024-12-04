using GameControllerForZwift.Core;
using GameControllerForZwift.UI.WPF.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace GameControllerForZwift.UI.WPF.Controls
{
    public class InputMapper : UserControl
    {
        //#region Fields
        //public ZwiftFunctionSelectorViewModel ViewModel { get; }

        //#endregion

        //#region Constructor

        //public InputMapper(ZwiftFunctionSelectorViewModel viewModel)
        //{
        //    ViewModel = viewModel;
        //    DataContext = this;
        //}

        //#endregion


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
                new PropertyMetadata(ZwiftFunction.ShowMenu, OnSelectedFunctionChanged));

        // Dependency property for ZwiftPlayerView selection
        public ZwiftPlayerView? SelectedPlayerView
        {
            get { return (ZwiftPlayerView?)GetValue(SelectedPlayerViewProperty); }
            set { SetValue(SelectedPlayerViewProperty, value); }
        }
        public static readonly DependencyProperty SelectedPlayerViewProperty =
            DependencyProperty.Register(nameof(SelectedPlayerView), typeof(ZwiftPlayerView?), typeof(InputMapper));

        // Dependency property for ZwiftRiderAction selection
        public ZwiftRiderAction? SelectedRiderAction
        {
            get { return (ZwiftRiderAction?)GetValue(SelectedRiderActionProperty); }
            set { SetValue(SelectedRiderActionProperty, value); }
        }
        public static readonly DependencyProperty SelectedRiderActionProperty =
            DependencyProperty.Register(nameof(SelectedRiderAction), typeof(ZwiftRiderAction?), typeof(InputMapper));

        private static void OnSelectedFunctionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (InputMapper)d;
            var selectedFunction = (ZwiftFunction)e.NewValue;

            // Reset secondary ComboBox selections based on function
            if (selectedFunction != ZwiftFunction.AdjustCameraAngle)
                control.SelectedPlayerView = null;

            if (selectedFunction != ZwiftFunction.RiderAction)
                control.SelectedRiderAction = null;
        }
    }
}
