using GameControllerForZwift.UI.WPF.Dialogs;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Threading;

namespace GameControllerForZwift.UI.WPF.Tests.Dialogs
{
    /// <summary>
    /// The message and name dialogs (CFG-028, CFG-038).
    /// </summary>
    [Collection(GameControllerForZwift.UnitTests.WpfWindows.Name)]
    public class DialogWindowTests
    {
        private static void Invoke(Button button) =>
            ((IInvokeProvider)new ButtonAutomationPeer(button).GetPattern(PatternInterface.Invoke)).Invoke();

        private static T Find<T>(FrameworkElement root, string name) where T : class =>
            (T)root.FindName(name);

        /// <summary>
        /// Shows the dialog modally and runs the action once it is open.
        /// </summary>
        private static bool? ShowAnd(Window window, Action<Window> action)
        {
            window.ShowInTaskbar = false;
            window.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () => action(window));
            return window.ShowDialog();
        }

        [Fact]
        public void MessageDialog_WithoutPrimaryOrSecondary_ShowsOnlyCloseButton()
        {
            GameControllerForZwift.UnitTests.TestApplication.Run(() =>
            {
                var dialog = new MessageDialogWindow("Title", "Body", null, null, "OK");

                Assert.Equal(Visibility.Collapsed, Find<Button>(dialog, "PrimaryButton").Visibility);
                Assert.Equal(Visibility.Collapsed, Find<Button>(dialog, "SecondaryButton").Visibility);
                Assert.True(Find<Button>(dialog, "CloseButton").IsDefault);
                Assert.Equal(1, Find<System.Windows.Controls.Primitives.UniformGrid>(dialog, "ButtonPanel").Columns);
                Assert.Equal("Title", dialog.Title);
                Assert.Equal("Body", Find<TextBlock>(dialog, "MessageText").Text);
                Assert.Equal(MessageDialogResult.Close, dialog.Result);
            });
        }

        [Fact]
        public void MessageDialog_WithAllButtons_ShowsThreeColumns()
        {
            GameControllerForZwift.UnitTests.TestApplication.Run(() =>
            {
                var dialog = new MessageDialogWindow("Title", "Body", "Save", "Don't save", "Cancel");

                Assert.Equal(3, Find<System.Windows.Controls.Primitives.UniformGrid>(dialog, "ButtonPanel").Columns);
                Assert.Equal("Save", Find<Button>(dialog, "PrimaryButton").Content);
                Assert.Equal("Don't save", Find<Button>(dialog, "SecondaryButton").Content);
                Assert.Equal("Cancel", Find<Button>(dialog, "CloseButton").Content);
            });
        }

        [Fact]
        public void MessageDialog_PrimaryClick_ReturnsPrimary()
        {
            GameControllerForZwift.UnitTests.TestApplication.Run(() =>
            {
                var dialog = new MessageDialogWindow("Title", "Body", "Save", "Don't save", "Cancel");

                ShowAnd(dialog, w => Invoke(Find<Button>(w, "PrimaryButton")));

                Assert.Equal(MessageDialogResult.Primary, dialog.Result);
            });
        }

        [Fact]
        public void MessageDialog_SecondaryClick_ReturnsSecondary()
        {
            GameControllerForZwift.UnitTests.TestApplication.Run(() =>
            {
                var dialog = new MessageDialogWindow("Title", "Body", "Save", "Don't save", "Cancel");

                ShowAnd(dialog, w => Invoke(Find<Button>(w, "SecondaryButton")));

                Assert.Equal(MessageDialogResult.Secondary, dialog.Result);
            });
        }

        [Fact]
        public void MessageDialog_CloseClick_ReturnsClose()
        {
            GameControllerForZwift.UnitTests.TestApplication.Run(() =>
            {
                var dialog = new MessageDialogWindow("Title", "Body", "Save", "Don't save", "Cancel");

                ShowAnd(dialog, w => Invoke(Find<Button>(w, "CloseButton")));

                Assert.Equal(MessageDialogResult.Close, dialog.Result);
            });
        }

        [Fact]
        public void CFG028_NamePrompt_EmptyName_DisablesConfirmWithoutMessage()
        {
            GameControllerForZwift.UnitTests.TestApplication.Run(() =>
            {
                var dialog = new NamePromptWindow("Save", "Name it", string.Empty, "Save", name => name.Trim().Length == 0 ? "Enter a name." : null);

                Assert.False(Find<Button>(dialog, "OkButton").IsEnabled);
                Assert.Equal(Visibility.Collapsed, Find<TextBlock>(dialog, "ValidationText").Visibility);
            });
        }

        [Fact]
        public void CFG028_NamePrompt_InvalidName_ShowsMessageAndDisablesConfirm()
        {
            GameControllerForZwift.UnitTests.TestApplication.Run(() =>
            {
                var dialog = new NamePromptWindow("Save", "Name it", "Climbing", "Save", name => name == "Climbing" ? "Name taken." : null);

                Assert.False(Find<Button>(dialog, "OkButton").IsEnabled);
                var validation = Find<TextBlock>(dialog, "ValidationText");
                Assert.Equal(Visibility.Visible, validation.Visibility);
                Assert.Equal("Name taken.", validation.Text);
            });
        }

        [Fact]
        public void CFG028_NamePrompt_TypingValidName_EnablesConfirmAndClearsMessage()
        {
            GameControllerForZwift.UnitTests.TestApplication.Run(() =>
            {
                var dialog = new NamePromptWindow("Save", "Name it", "Climbing", "Save", name => name == "Climbing" ? "Name taken." : null);

                Find<TextBox>(dialog, "NameTextBox").Text = "Flat";

                Assert.True(Find<Button>(dialog, "OkButton").IsEnabled);
                Assert.Equal(string.Empty, Find<TextBlock>(dialog, "ValidationText").Text);
                Assert.Equal("Flat", dialog.EnteredName);
            });
        }

        [Fact]
        public void CFG028_NamePrompt_ConfirmWithValidName_ClosesWithTrue()
        {
            GameControllerForZwift.UnitTests.TestApplication.Run(() =>
            {
                var dialog = new NamePromptWindow("Save", "Name it", "Flat", "Save", _ => null);

                var result = ShowAnd(dialog, w => Invoke(Find<Button>(w, "OkButton")));

                Assert.True(result);
                Assert.Equal("Flat", dialog.EnteredName);
            });
        }

        [Fact]
        public void CFG028_NamePrompt_ConfirmWithInvalidName_StaysOpen()
        {
            GameControllerForZwift.UnitTests.TestApplication.Run(() =>
            {
                var dialog = new NamePromptWindow("Save", "Name it", "Flat", "Save", name => name == "Flat" ? null : "Bad.");

                var result = ShowAnd(dialog, w =>
                {
                    var box = Find<TextBox>(w, "NameTextBox");
                    box.Text = "Other";
                    // The confirm button is disabled, so click the handler the way a default-button Enter would.
                    typeof(NamePromptWindow).GetMethod("OkButton_Click", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                        .Invoke(w, [w, new RoutedEventArgs()]);
                    Assert.True(w.IsVisible);
                    w.Close();
                });

                Assert.NotEqual(true, result);
            });
        }

        [Fact]
        public void NamePrompt_NullValidator_Throws()
        {
            GameControllerForZwift.UnitTests.TestApplication.Run(() =>
            {
                Assert.Throws<ArgumentNullException>(() => new NamePromptWindow("T", "M", "N", "OK", null!));
            });
        }
    }
}
