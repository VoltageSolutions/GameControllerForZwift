using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GameControllerForZwift.UI.WPF.Controls
{
    /// <summary>What an <see cref="ActionRow"/> shows at its trailing edge.</summary>
    public enum ActionRowTrailing
    {
        /// <summary>Nothing.</summary>
        None,

        /// <summary>A chevron: the row opens another screen.</summary>
        Chevron,

        /// <summary>An arrow out of a box: the row opens something outside the app.</summary>
        External,
    }

    /// <summary>
    /// A row-sized button: a title, an optional line under it, and an optional chevron or external-link mark
    /// (docs/spec/ux/visual-system.md, Surfaces). The title is the button's content, so a screen reader reads it as the name.
    /// </summary>
    public class ActionRow : Button
    {
        /// <summary>Identifies <see cref="Title"/>.</summary>
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
            nameof(Title), typeof(string), typeof(ActionRow), new PropertyMetadata(string.Empty, OnTitleChanged));

        /// <summary>Identifies <see cref="Description"/>.</summary>
        public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
            nameof(Description), typeof(string), typeof(ActionRow), new PropertyMetadata(string.Empty));

        /// <summary>Identifies <see cref="Trailing"/>.</summary>
        public static readonly DependencyProperty TrailingProperty = DependencyProperty.Register(
            nameof(Trailing), typeof(ActionRowTrailing), typeof(ActionRow), new PropertyMetadata(ActionRowTrailing.None));


        static ActionRow()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ActionRow), new FrameworkPropertyMetadata(typeof(ActionRow)));
        }

        /// <summary>Gets or sets the row's title.</summary>
        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        /// <summary>Gets or sets the line under the title.</summary>
        public string Description
        {
            get => (string)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        /// <summary>Gets or sets the mark at the trailing edge.</summary>
        public ActionRowTrailing Trailing
        {
            get => (ActionRowTrailing)GetValue(TrailingProperty);
            set => SetValue(TrailingProperty, value);
        }


        private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((ActionRow)d).Content = e.NewValue;
    }
}
