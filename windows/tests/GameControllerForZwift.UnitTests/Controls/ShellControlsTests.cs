using GameControllerForZwift.UI.WPF.Controls;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;

namespace GameControllerForZwift.UnitTests.Controls
{
    /// <summary>Roles screen readers hear for the shell (docs/spec/ux/accessibility.md, core IDs).</summary>
    [Collection(WpfWindows.Name)]
    public class ShellControlsTests
    {
        private static readonly string[] Tabs = ["Ride", "Buttons", "Connections"];

        [Fact]
        public void NavTabList_IsATabListOfTabs()
        {
            TestApplication.Run(() =>
            {
                var list = new NavTabList { ItemsSource = Tabs };
                var window = new System.Windows.Window { Content = list, Width = 200, Height = 200, ShowActivated = false, ShowInTaskbar = false };
                window.Show();
                try
                {
                    AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(list);

                    Assert.Equal(AutomationControlType.Tab, peer.GetAutomationControlType());
                    AutomationPeer first = Assert.IsAssignableFrom<AutomationPeer>(peer.GetChildren()[0]);
                    Assert.Equal(AutomationControlType.TabItem, first.GetAutomationControlType());
                    Assert.NotNull(first.GetPattern(PatternInterface.SelectionItem));
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void MenuPillButton_IsAMenuItemThatInvokes()
        {
            TestApplication.Run(() =>
            {
                var button = new MenuPillButton();
                bool clicked = false;
                button.Click += (_, _) => clicked = true;
                AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(button);

                Assert.Equal(AutomationControlType.MenuItem, peer.GetAutomationControlType());
                ((IInvokeProvider)peer.GetPattern(PatternInterface.Invoke)).Invoke();
                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle, () => { });
                Assert.True(clicked);
            });
        }

        [Fact]
        public void MoreMenuButton_ReportsExpandAndCollapse()
        {
            TestApplication.Run(() =>
            {
                var button = new MoreMenuButton();
                int clicks = 0;
                button.Click += (_, _) =>
                {
                    clicks++;
                    button.IsExpanded = !button.IsExpanded;
                };
                AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(button);
                var expandCollapse = Assert.IsAssignableFrom<IExpandCollapseProvider>(peer.GetPattern(PatternInterface.ExpandCollapse));
                Assert.NotNull(peer.GetPattern(PatternInterface.Invoke));

                Assert.Equal(ExpandCollapseState.Collapsed, expandCollapse.ExpandCollapseState);
                button.IsExpanded = true;
                Assert.Equal(ExpandCollapseState.Expanded, expandCollapse.ExpandCollapseState);

                expandCollapse.Expand();
                Pump();
                Assert.Equal(0, clicks);

                expandCollapse.Collapse();
                Pump();
                Assert.Equal(1, clicks);
                Assert.False(button.IsExpanded);

                expandCollapse.Collapse();
                Pump();
                Assert.Equal(1, clicks);

                expandCollapse.Expand();
                Pump();
                Assert.Equal(2, clicks);
                Assert.True(button.IsExpanded);
            });
        }

        private static void Pump() =>
            System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle, () => { });
    }
}
