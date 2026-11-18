using System.Windows;

namespace GameControllerForZwift.UI.WPF.Dialogs
{
    /// <summary>
    /// Which button closed a <see cref="MessageDialogWindow"/>.
    /// </summary>
    public enum MessageDialogResult
    {
        Primary,
        Secondary,
        Close,
    }

    /// <summary>
    /// A small Fluent-styled message dialog with up to three buttons. Enter chooses the primary button and Escape the close button.
    /// </summary>
    public partial class MessageDialogWindow : Window
    {
        public MessageDialogWindow(string title, string message, string? primaryText, string? secondaryText, string closeText)
        {
            InitializeComponent();

            Title = title;
            HeadingText.Text = title;
            MessageText.Text = message;
            CloseButton.Content = closeText;

            if (string.IsNullOrEmpty(primaryText))
            {
                PrimaryButton.Visibility = Visibility.Collapsed;
                CloseButton.IsDefault = true;
            }
            else
            {
                PrimaryButton.Content = primaryText;
            }

            if (string.IsNullOrEmpty(secondaryText))
                SecondaryButton.Visibility = Visibility.Collapsed;
            else
                SecondaryButton.Content = secondaryText;

            ButtonPanel.Columns = (PrimaryButton.Visibility == Visibility.Visible ? 1 : 0)
                + (SecondaryButton.Visibility == Visibility.Visible ? 1 : 0) + 1;

            Loaded += (_, _) => (PrimaryButton.Visibility == Visibility.Visible ? PrimaryButton : CloseButton).Focus();
        }

        /// <summary>
        /// Gets which button closed the dialog. Escape and the title bar close button count as <see cref="MessageDialogResult.Close"/>.
        /// </summary>
        public MessageDialogResult Result { get; private set; } = MessageDialogResult.Close;

        private void PrimaryButton_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageDialogResult.Primary;
            DialogResult = true;
        }

        private void SecondaryButton_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageDialogResult.Secondary;
            DialogResult = true;
        }
    }
}
