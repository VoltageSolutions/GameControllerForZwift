using System.Windows;
using System.Windows.Controls;

namespace GameControllerForZwift.UI.WPF.Controls
{
    /// <summary>
    /// The Page component (docs/spec/ux/style-system.md): the scroll area of a screen. It spans the width of the window
    /// and keeps the page padding inside the scroll area, so a card's shadow has room on every side (UX-V-051,
    /// UX-V-052). The padding and the room for the floating bottom bar come from the layout tokens.
    /// </summary>
    public class ScreenPage : ContentControl
    {
        static ScreenPage()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ScreenPage), new FrameworkPropertyMetadata(typeof(ScreenPage)));
        }
    }

    /// <summary>
    /// The Section component: a group label over its content, with the label gap between them (UX-V-047). The label is
    /// a heading for screen readers. Without a label, only the content shows.
    /// </summary>
    public class Section : ContentControl
    {
        /// <summary>Identifies <see cref="Label"/>.</summary>
        public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
            nameof(Label), typeof(string), typeof(Section), new PropertyMetadata(string.Empty));

        static Section()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Section), new FrameworkPropertyMetadata(typeof(Section)));
        }

        /// <summary>Gets or sets the label over the content.</summary>
        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }
    }

    /// <summary>
    /// The StatusDot component: an 8 px dot in <c>ok</c> when the thing works and <c>err</c> when it needs the person
    /// (UX-V-056). The dot never carries a status alone, so it always sits beside words.
    /// </summary>
    public class StatusDot : Control
    {
        /// <summary>Identifies <see cref="IsOk"/>.</summary>
        public static readonly DependencyProperty IsOkProperty = DependencyProperty.Register(
            nameof(IsOk), typeof(bool), typeof(StatusDot), new PropertyMetadata(false));

        static StatusDot()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(StatusDot), new FrameworkPropertyMetadata(typeof(StatusDot)));
        }

        /// <summary>Gets or sets whether the dot shows a working state (<c>ok</c>) or a problem (<c>err</c>).</summary>
        public bool IsOk
        {
            get => (bool)GetValue(IsOkProperty);
            set => SetValue(IsOkProperty, value);
        }
    }

    /// <summary>
    /// The StatusChip component: a 40 px pill with a status dot and words in <c>text</c> (UX-V-056, UX-V-057).
    /// </summary>
    public class StatusChip : Control
    {
        /// <summary>Identifies <see cref="IsOk"/>.</summary>
        public static readonly DependencyProperty IsOkProperty = DependencyProperty.Register(
            nameof(IsOk), typeof(bool), typeof(StatusChip), new PropertyMetadata(false));

        /// <summary>Identifies <see cref="Text"/>.</summary>
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            nameof(Text), typeof(string), typeof(StatusChip), new PropertyMetadata(string.Empty));

        static StatusChip()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(StatusChip), new FrameworkPropertyMetadata(typeof(StatusChip)));
        }

        /// <summary>Gets or sets whether the dot shows a working state (<c>ok</c>) or a problem (<c>err</c>).</summary>
        public bool IsOk
        {
            get => (bool)GetValue(IsOkProperty);
            set => SetValue(IsOkProperty, value);
        }

        /// <summary>Gets or sets the words beside the dot.</summary>
        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }
    }

    /// <summary>The Stat component: a number over a label, such as "32 min" over "ride time".</summary>
    public class Stat : Control
    {
        /// <summary>Identifies <see cref="Value"/>.</summary>
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value), typeof(string), typeof(Stat), new PropertyMetadata(string.Empty));

        /// <summary>Identifies <see cref="Label"/>.</summary>
        public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
            nameof(Label), typeof(string), typeof(Stat), new PropertyMetadata(string.Empty));

        static Stat()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Stat), new FrameworkPropertyMetadata(typeof(Stat)));
        }

        /// <summary>Gets or sets the number.</summary>
        public string Value
        {
            get => (string)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        /// <summary>Gets or sets the label under the number.</summary>
        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }
    }
}
