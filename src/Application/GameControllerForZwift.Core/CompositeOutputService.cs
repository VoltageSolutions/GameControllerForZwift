namespace GameControllerForZwift.Core
{
    /// <summary>
    /// Routes controller actions to the outputs ticked in <see cref="OutputOptions"/> (any combination, including none).
    /// Two paths, which <see cref="DataIntegrator"/> uses together:
    ///  * Continuous state path (<see cref="IZwiftControllerOutput"/>): <see cref="Handles"/> is true when "Zwift Controller Emulator"
    ///    is ticked and the LAN service can express the function; <see cref="UpdateAsync"/> forwards state to it (regardless of whether
    ///    Zwift is connected - the network service drops frames then). When the emulator is switched off one Neutral state is sent so nothing stays held.
    ///  * Discrete path (<see cref="IOutputService"/>): <see cref="PerformActionAsync"/> is KEYBOARD-ONLY by design. It presses the key when
    ///    "Keyboard" is ticked, otherwise it is a successful no-op ("not routed"). Functions without a key (Steer/Brake) are rejected by the keyboard.
    /// <see cref="SendsToKeyboard"/> tells the integrator to call the discrete path even for functions the emulator handles.
    /// </summary>
    public class CompositeOutputService : IOutputService, IZwiftControllerOutput
    {
        private readonly IOutputService _keyboard;
        private readonly ILanOutputService _network;
        private readonly OutputOptions _options;
        private volatile bool _emulatorHasState;

        public CompositeOutputService(IOutputService keyboard, ILanOutputService network, OutputOptions options)
        {
            _keyboard = keyboard;
            _network = network;
            _options = options;
        }

        /// <inheritdoc />
        public bool SendsToKeyboard => _options.SendToKeyboard;

        /// <summary>Keyboard-only: presses the mapped key if "Keyboard" is ticked, otherwise does nothing (non-failing result).</summary>
        public Task<ActionResult> PerformActionAsync(ZwiftFunction zwiftFunction, ZwiftPlayerView playerView = ZwiftPlayerView.Default, ZwiftRiderAction riderAction = ZwiftRiderAction.WaveHand)
        {
            if (_options.SendToKeyboard)
                return _keyboard.PerformActionAsync(zwiftFunction, playerView, riderAction);
            return Task.FromResult(new ActionResult { Success = true, ErrorMessage = "Not routed: keyboard output is disabled." });
        }

        /// <inheritdoc />
        public bool Handles(ZwiftFunction function, ZwiftRiderAction riderAction)
            => _options.SendToZwiftEmulator && _network.CanHandle(function, riderAction);

        /// <inheritdoc />
        public Task UpdateAsync(ZwiftControllerState state, CancellationToken ct = default)
        {
            if (_options.SendToZwiftEmulator)
            {
                _emulatorHasState = true;
                return _network.UpdateAsync(state, ct);
            }
            if (_emulatorHasState)
            {
                _emulatorHasState = false; // emulator was just switched off: release everything once
                return _network.UpdateAsync(ZwiftControllerState.Neutral, ct);
            }
            return Task.CompletedTask;
        }
    }
}
