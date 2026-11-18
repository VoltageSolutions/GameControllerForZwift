using GameControllerForZwift.Core;
using GameControllerForZwift.Core.Controller;
using GameControllerForZwift.UI.WPF.Tests.ViewModels;
using GameControllerForZwift.UI.WPF.ViewModels;
using GameControllerForZwift.UI.WPF.Views;
using GameControllerForZwift.UnitTests;
using NSubstitute;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Threading;
using static GameControllerForZwift.UI.WPF.Tests.ViewModels.ConfigurationTestContext;

namespace GameControllerForZwift.UI.WPF.Tests.Views
{
    /// <summary>The Connections page as the user sees it (SCR-CONN-001 to SCR-CONN-011).</summary>
    [Collection(WpfWindows.Name)]
    public sealed class ConnectionsPageTests : IDisposable
    {
        private readonly ConfigurationTestContext _context = new();

        public void Dispose() => _context.Dispose();

        private sealed record Setup(ConnectionsPage Page, ConnectionsViewModel ViewModel, List<string> Opened, IZwiftConnection Connection);

        private Setup Show(bool zwiftConnected, params IController[] controllers)
        {
            var connection = Substitute.For<IZwiftConnection>();
            connection.IsZwiftConnected.Returns(zwiftConnected);
            var status = new ZwiftStatus(connection);
            var opened = new List<string>();
            var viewModel = new ConnectionsViewModel(status, _context.CreateViewModel(controllers), opened.Add);
            var page = new ConnectionsPage { DataContext = viewModel };
            Layout(page);
            return new Setup(page, viewModel, opened, connection);
        }

        private static void Layout(FrameworkElement page)
        {
            page.Measure(new Size(900, 1200));
            page.Arrange(new Rect(0, 0, 900, 1200));
            page.UpdateLayout();
            for (int i = 0; i < 4; i++)
            {
                page.Dispatcher.Invoke(DispatcherPriority.ApplicationIdle, () => { });
            }
        }

        private static T Element<T>(FrameworkElement page, string name) where T : class => Assert.IsAssignableFrom<T>(page.FindName(name));

        private static void Click(Button button)
        {
            var invoke = (IInvokeProvider)new ButtonAutomationPeer(button).GetPattern(PatternInterface.Invoke)!;
            invoke.Invoke();
            button.Dispatcher.Invoke(DispatcherPriority.ApplicationIdle, () => { });
        }

        private static string Text(FrameworkElement page, string automationId) =>
            AllTextBlocks(page).First(t => AutomationProperties.GetAutomationId(t) == automationId).Text;

        private static IEnumerable<TextBlock> AllTextBlocks(DependencyObject root)
        {
            int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = System.Windows.Media.VisualTreeHelper.GetChild(root, i);
                if (child is TextBlock text)
                {
                    yield return text;
                }

                foreach (TextBlock nested in AllTextBlocks(child))
                {
                    yield return nested;
                }
            }
        }

        [Fact]
        public void SCRCONN001_ShowsTheControllersNameAndState()
        {
            TestApplication.Run(() =>
            {
                Setup setup = Show(false, Controller(Xbox));

                Assert.Equal("Your Xbox Series X Controller is here", Text(setup.Page, "ConnectionsControllerName"));
                Assert.Equal("Awake and connected", Text(setup.Page, "ConnectionsControllerState"));
            });
        }

        [Fact]
        public void SCRCONN003_WithNoController_ShowsWakeYourControllerAndBothWaysOut()
        {
            TestApplication.Run(() =>
            {
                Setup setup = Show(false);

                Assert.Equal("Wake your controller", Text(setup.Page, "ConnectionsControllerName"));
                Assert.Equal("Looking for your controller...", Text(setup.Page, "ConnectionsControllerState"));
                Assert.Equal(Visibility.Visible, Element<Button>(setup.Page, "PairBluetoothButton").Visibility);
                Assert.False(Element<ComboBox>(setup.Page, "ControllerPicker").IsEnabled);
                Assert.Equal(Visibility.Visible, Element<ComboBox>(setup.Page, "ControllerPicker").Visibility);
            });
        }

        [Fact]
        public void SCRCONN002_ThePickerIsEnabledWithTwoControllersAndSelectsTheChoice()
        {
            TestApplication.Run(() =>
            {
                IController dualSense = Controller(DualSense);
                Setup setup = Show(false, Controller(Xbox), dualSense);
                var picker = Element<ComboBox>(setup.Page, "ControllerPicker");

                Assert.True(picker.IsEnabled);
                Assert.Equal(2, picker.Items.Count);
                Assert.Equal("Name", picker.DisplayMemberPath);

                picker.SelectedItem = dualSense;

                Assert.Same(dualSense, setup.ViewModel.SelectedController);
                Assert.Equal("Your DualSense Wireless Controller is here", Text(setup.Page, "ConnectionsControllerName"));
            });
        }

        [Fact]
        public void SCRCONN002_PairANewBluetoothControllerOpensWindowsBluetoothSettings()
        {
            TestApplication.Run(() =>
            {
                Setup setup = Show(false);

                Click(Element<Button>(setup.Page, "PairBluetoothButton"));

                Assert.Equal(["ms-settings:bluetooth"], setup.Opened);
            });
        }

        [Fact]
        public void SCRCONN009_NotConnected_ShowsThreeStepsEachWithItsStateInWords()
        {
            TestApplication.Run(() =>
            {
                Setup setup = Show(false);
                var steps = Element<ItemsControl>(setup.Page, "ZwiftSteps");

                Assert.Equal(Visibility.Visible, steps.Visibility);
                Assert.Equal(3, steps.Items.Count);
                Assert.Equal("Power Source: Voltage Gamepad", Text(setup.Page, "ConnectionsStep1"));
                Assert.Equal("Not done yet", Text(setup.Page, "ConnectionsStep1State"));
                Assert.Equal("Controls: Voltage Gamepad", Text(setup.Page, "ConnectionsStep2"));
                Assert.Equal("Power Source: your trainer", Text(setup.Page, "ConnectionsStep3"));
                Assert.Equal(Visibility.Collapsed, Element<Button>(setup.Page, "ShowMeHowButton").Visibility);
            });
        }

        [Fact]
        public void SCRCONN011_Connected_CollapsesTheSteps_AndShowMeHowTogglesThem()
        {
            TestApplication.Run(() =>
            {
                Setup setup = Show(true, Controller(Xbox));
                var steps = Element<ItemsControl>(setup.Page, "ZwiftSteps");
                var showMeHow = Element<Button>(setup.Page, "ShowMeHowButton");

                Assert.Equal("Zwift can see your controller", Text(setup.Page, "ConnectionsZwiftStatus"));
                Assert.Equal(Visibility.Collapsed, steps.Visibility);
                Assert.Equal(Visibility.Visible, showMeHow.Visibility);
                Assert.Equal("Show me how", showMeHow.Content);

                Click(showMeHow);

                Assert.Equal(Visibility.Visible, steps.Visibility);
                Assert.Equal(Visibility.Visible, showMeHow.Visibility);
                Assert.Equal("Hide steps", showMeHow.Content);
                Assert.Equal("Hide steps", AutomationProperties.GetName(showMeHow));
                Assert.Equal("Done", Text(setup.Page, "ConnectionsStep1State"));
                Assert.Equal("Not done yet", Text(setup.Page, "ConnectionsStep2State"));
                Assert.Equal("Not done yet", Text(setup.Page, "ConnectionsStep3State"));

                Click(showMeHow);

                Assert.Equal(Visibility.Collapsed, steps.Visibility);
                Assert.Equal("Show me how", showMeHow.Content);
            });
        }

        [Fact]
        public void SCRCONN007_NoActionUsesTheAccentFill()
        {
            TestApplication.Run(() =>
            {
                Setup setup = Show(true, Controller(Xbox));

                Assert.NotSame(Application.Current.FindResource("Button.Primary"), Element<Button>(setup.Page, "ShowMeHowButton").Style);
                Assert.NotSame(Application.Current.FindResource("Button.Primary"), Element<Button>(setup.Page, "PairBluetoothButton").Style);
            });
        }

        [Fact]
        public void SCRCONN005_TheStatusIsALiveRegion_AndChangesAreAnnounced()
        {
            TestApplication.Run(() =>
            {
                Setup setup = Show(false);
                var status = Element<TextBlock>(setup.Page, "ZwiftStatusText");
                Assert.Equal(AutomationLiveSetting.Polite, AutomationProperties.GetLiveSetting(status));
                Assert.Equal("Zwift isn't connected yet", status.Text);

                setup.Connection.ConnectionChanged += NSubstitute.Raise.Event<EventHandler<bool>>(setup.Connection, true);
                Layout(setup.Page);

                Assert.Equal("Zwift can see your controller", status.Text);
                Assert.Equal(Visibility.Collapsed, Element<ItemsControl>(setup.Page, "ZwiftSteps").Visibility);
            });
        }

        [Fact]
        public void SCRCONN004_TheCardsHaveTheirAutomationIds()
        {
            TestApplication.Run(() =>
            {
                Setup setup = Show(false);

                Assert.Equal("ConnectionsZwiftCard", AutomationProperties.GetAutomationId(Element<Border>(setup.Page, "ConnectionsZwiftCard")));
                Assert.Equal("ConnectionsShowMeHowButton", AutomationProperties.GetAutomationId(Element<Button>(setup.Page, "ShowMeHowButton")));
                Assert.Equal("ConnectionsControllerPicker", AutomationProperties.GetAutomationId(Element<ComboBox>(setup.Page, "ControllerPicker")));
            });
        }
    }
}
