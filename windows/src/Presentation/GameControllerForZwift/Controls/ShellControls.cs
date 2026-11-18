using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;

namespace GameControllerForZwift.UI.WPF.Controls
{
    /// <summary>
    /// The list of tabs. Screen readers hear a tab list and tabs, "1 of 3" and so on (docs/spec/ux/accessibility.md,
    /// core IDs). Selection works like a list box.
    /// </summary>
    public class NavTabList : ListBox
    {
        protected override AutomationPeer OnCreateAutomationPeer() => new NavTabListAutomationPeer(this);

        private sealed class NavTabListAutomationPeer : ListBoxAutomationPeer
        {
            public NavTabListAutomationPeer(ListBox owner)
                : base(owner)
            {
            }

            protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Tab;

            protected override ItemAutomationPeer CreateItemAutomationPeer(object item) => new NavTabAutomationPeer(item, this);
        }

        private sealed class NavTabAutomationPeer : ListBoxItemAutomationPeer
        {
            public NavTabAutomationPeer(object owner, SelectorAutomationPeer selectorAutomationPeer)
                : base(owner, selectorAutomationPeer)
            {
            }

            protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.TabItem;
        }
    }

    /// <summary>An item of the More menu. Screen readers hear a menu item.</summary>
    public class MenuPillButton : Button
    {
        protected override AutomationPeer OnCreateAutomationPeer() => new MenuPillAutomationPeer(this);

        private sealed class MenuPillAutomationPeer : ButtonAutomationPeer
        {
            public MenuPillAutomationPeer(Button owner)
                : base(owner)
            {
            }

            protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.MenuItem;
        }
    }

    /// <summary>
    /// The More button. Screen readers hear a button that expands and whether the menu is open (docs/spec/ux/accessibility.md,
    /// core IDs).
    /// </summary>
    public class MoreMenuButton : Button
    {
        public static readonly DependencyProperty IsExpandedProperty = DependencyProperty.Register(
            nameof(IsExpanded), typeof(bool), typeof(MoreMenuButton), new PropertyMetadata(false, OnIsExpandedChanged));

        /// <summary>True while the menu is open.</summary>
        public bool IsExpanded
        {
            get => (bool)GetValue(IsExpandedProperty);
            set => SetValue(IsExpandedProperty, value);
        }

        protected override AutomationPeer OnCreateAutomationPeer() => new MoreMenuButtonAutomationPeer(this);

        private static void OnIsExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (UIElementAutomationPeer.FromElement((MoreMenuButton)d) is MoreMenuButtonAutomationPeer peer)
            {
                peer.RaisePropertyChangedEvent(
                    ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
                    ToState((bool)e.OldValue),
                    ToState((bool)e.NewValue));
            }
        }

        internal static ExpandCollapseState ToState(bool isExpanded) =>
            isExpanded ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;

        private sealed class MoreMenuButtonAutomationPeer : ButtonAutomationPeer, IExpandCollapseProvider
        {
            private readonly MoreMenuButton _owner;

            public MoreMenuButtonAutomationPeer(MoreMenuButton owner)
                : base(owner)
            {
                _owner = owner;
            }

            public ExpandCollapseState ExpandCollapseState => ToState(_owner.IsExpanded);

            public override object GetPattern(PatternInterface patternInterface) =>
                patternInterface == PatternInterface.ExpandCollapse ? this : base.GetPattern(patternInterface);

            // Expand and collapse click the button, which toggles the menu, when the menu is not already in that state.
            public void Expand()
            {
                if (!_owner.IsExpanded)
                {
                    ((IInvokeProvider)this).Invoke();
                }
            }

            public void Collapse()
            {
                if (_owner.IsExpanded)
                {
                    ((IInvokeProvider)this).Invoke();
                }
            }
        }
    }
}
