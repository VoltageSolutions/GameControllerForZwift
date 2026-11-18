using System.Windows;
using System.Windows.Controls;

namespace GameControllerForZwift.UI.WPF.Dialogs
{
    /// <summary>
    /// Asks for a configuration name. The name is validated as the user types; the message shows inline and the confirm
    /// button stays disabled until the name is valid (CFG-028). Enter confirms and Escape cancels.
    /// </summary>
    public partial class NamePromptWindow : Window
    {
        private readonly Func<string, string?> _validate;

        public NamePromptWindow(string title, string message, string initialName, string confirmText, Func<string, string?> validate)
        {
            ArgumentNullException.ThrowIfNull(validate);
            _validate = validate;
            InitializeComponent();

            Title = title;
            HeadingText.Text = title;
            MessageText.Text = message;
            OkButton.Content = confirmText;
            NameTextBox.Text = initialName ?? string.Empty;
            Validate();

            Loaded += (_, _) =>
            {
                NameTextBox.Focus();
                NameTextBox.SelectAll();
            };
        }

        /// <summary>
        /// Gets the name the user confirmed.
        /// </summary>
        public string EnteredName => NameTextBox.Text;

        private void NameTextBox_TextChanged(object sender, TextChangedEventArgs e) => Validate();

        private void Validate()
        {
            var error = _validate(NameTextBox.Text);

            // An empty box only disables the button; a message for it would appear before the user has typed anything.
            bool showMessage = error is not null && NameTextBox.Text.Trim().Length > 0;
            ValidationText.Text = showMessage ? error : string.Empty;
            ValidationText.Visibility = showMessage ? Visibility.Visible : Visibility.Collapsed;
            OkButton.IsEnabled = error is null;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (_validate(NameTextBox.Text) is null)
                DialogResult = true;
        }
    }
}
