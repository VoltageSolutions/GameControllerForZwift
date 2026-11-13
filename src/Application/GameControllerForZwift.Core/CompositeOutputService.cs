namespace GameControllerForZwift.Core
{
    /// <summary>
    /// Routes actions to the keyboard or the Zwift LAN controller.
    /// Rules (when <see cref="OutputOptions.UseZwiftLanController"/> is on):
    ///  1. While Zwift is connected to the LAN controller (<see cref="ILanOutputService.IsAvailable"/>) and it has a mapping
    ///     for the function, the function goes to the LAN service.
    ///  2. Otherwise (Zwift not connected, or no mapping) it falls back to the keyboard (e.g. ShiftUp/ShiftDown -> PageUp/PageDown).
    ///     Steer and Brake have no keyboard shortcut, so they are simply unhandled there.
    /// When the option is off, everything goes to the keyboard.
    /// </summary>
    public class CompositeOutputService : IOutputService, IZwiftControllerOutput
    {
        private readonly IOutputService _keyboard;
        private readonly ILanOutputService _network;
        private readonly OutputOptions _options;

        public CompositeOutputService(IOutputService keyboard, ILanOutputService network, OutputOptions options)
        {
            _keyboard = keyboard;
            _network = network;
            _options = options;
        }

        public Task<ActionResult> PerformActionAsync(ZwiftFunction zwiftFunction, ZwiftPlayerView playerView = ZwiftPlayerView.Default, ZwiftRiderAction riderAction = ZwiftRiderAction.WaveHand)
        {
            return Route(zwiftFunction, riderAction).PerformActionAsync(zwiftFunction, playerView, riderAction);
        }

        /// <inheritdoc />
        public bool Handles(ZwiftFunction function, ZwiftRiderAction riderAction)
            => _options.UseZwiftLanController && _network.IsAvailable && _network.CanHandle(function, riderAction);

        /// <inheritdoc />
        public Task UpdateAsync(ZwiftControllerState state, CancellationToken ct = default)
            => _options.UseZwiftLanController ? _network.UpdateAsync(state, ct) : Task.CompletedTask;

        public IOutputService Route(ZwiftFunction zwiftFunction, ZwiftRiderAction riderAction = ZwiftRiderAction.WaveHand)
        {
            if (_options.UseZwiftLanController && _network.IsAvailable && _network.Supports(zwiftFunction, riderAction))
                return _network;
            return _keyboard;
        }
    }
}
