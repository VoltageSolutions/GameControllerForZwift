using CommunityToolkit.Mvvm.ComponentModel;
using GameControllerForZwift.Core;
using GameControllerForZwift.UI.WPF.Localization;
using System.Windows.Threading;

namespace GameControllerForZwift.UI.WPF.ViewModels
{
    /// <summary>
    /// Whether Zwift is connected, for the UI thread. The shell shows the Connections tab dot from it (UX-N-002) and the
    /// Connections screen shows its status (SCR-CONN-004).
    /// </summary>
    public sealed partial class ZwiftStatus : ObservableObject, IDisposable
    {
        private readonly IZwiftConnection? _connection;
        private readonly Dispatcher _dispatcher;

        /// <summary>Follows <paramref name="connection"/>. Without one, Zwift shows as not connected.</summary>
        public ZwiftStatus(IZwiftConnection? connection = null)
        {
            _connection = connection;
            _dispatcher = Dispatcher.CurrentDispatcher;
            _isConnected = connection?.IsZwiftConnected ?? false;
            if (connection is not null)
            {
                connection.ConnectionChanged += OnConnectionChanged;
            }
        }

        /// <summary>True while Zwift is connected.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(StatusText))]
        private bool _isConnected;

        /// <summary>The status in words.</summary>
        public string StatusText => Localizer.Get(IsConnected ? "ZwiftConnected" : "ZwiftNotConnected");

        // The network raises the event on its own thread. The change is posted to the thread that created this object.
        private void OnConnectionChanged(object? sender, bool connected)
        {
            if (_dispatcher.CheckAccess())
            {
                IsConnected = connected;
            }
            else
            {
                _dispatcher.BeginInvoke(new Action(() => IsConnected = connected));
            }
        }

        public void Dispose()
        {
            if (_connection is not null)
            {
                _connection.ConnectionChanged -= OnConnectionChanged;
            }
        }
    }
}
