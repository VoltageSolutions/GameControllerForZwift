namespace GameControllerForZwift.Core
{
    /// <summary>
    /// Runtime options choosing which output(s) receive controller actions. Registered as a singleton and read on every
    /// poll (25 ms, pool thread) while being written from the UI thread, so the flags are volatile and changes take effect immediately.
    /// Any combination is valid: both, either, or neither. Persisted by <see cref="OutputOptionsStore"/>.
    /// </summary>
    public class OutputOptions
    {
        private volatile bool _sendToZwiftEmulator = true;
        private volatile bool _sendToKeyboard = false;

        /// <summary>Raised (on the writing thread) after either flag actually changed.</summary>
        public event EventHandler? Changed;

        /// <summary>Send actions to the emulated Zwift controller (Wahoo DirCon over LAN). Default: true.</summary>
        public bool SendToZwiftEmulator
        {
            get => _sendToZwiftEmulator;
            set
            {
                if (_sendToZwiftEmulator == value) return;
                _sendToZwiftEmulator = value;
                Changed?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>Send actions as keyboard key presses. Default: false.</summary>
        public bool SendToKeyboard
        {
            get => _sendToKeyboard;
            set
            {
                if (_sendToKeyboard == value) return;
                _sendToKeyboard = value;
                Changed?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
