using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Data;

namespace GameControllerForZwift.UI.WPF.Views
{
    /// <summary>The Connections tab.</summary>
    public partial class ConnectionsPage : UserControl
    {
        public ConnectionsPage()
        {
            InitializeComponent();
        }

        // SCR-CONN-005: screen readers announce the Zwift status when it changes.
        private void ZwiftStatusText_TargetUpdated(object? sender, DataTransferEventArgs e)
        {
            AutomationPeer? peer = UIElementAutomationPeer.FromElement(ZwiftStatusText)
                ?? UIElementAutomationPeer.CreatePeerForElement(ZwiftStatusText);
            peer?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
        }
    }
}
