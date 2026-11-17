using GameControllerForZwift.Core.Controller;
using GameControllerForZwift.Core;
using Microsoft.Extensions.Logging;
using static SDL2.SDL;

namespace GameControllerForZwift.Gamepad.SDL2
{
    public sealed partial class SDL2InputService : IInputService, IDisposable
    {
        #region Fields

        // Joystick and game controller events: SDL_JOYAXISMOTION (0x600) up to the end of that block.
        private const SDL_EventType FirstJoystickEvent = (SDL_EventType)0x600;
        private const SDL_EventType LastJoystickEvent = (SDL_EventType)0x6FF;

        private readonly ILogger<SDL2InputService> _logger;
        private readonly List<IController> _controllers = new();
        private readonly Dictionary<int, IController> _controllersByInstanceId = new();
        private HashSet<int>? _knownDeviceIds;
        private long? _lastCheckTicks;
        private readonly Lock _sync = new();
        private bool _isInitialized;
        private bool _isDisposed;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes the SDL2 input service.
        /// </summary>
        /// <param name="logger">The logger for capturing events and errors.</param>
        public SDL2InputService(ILogger<SDL2InputService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            InitializeSDL();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the currently connected game controllers. A controller that stays connected is returned as the same
        /// instance on every call. Only devices that connected since the last call are opened, and only devices that
        /// disconnected are closed, so calling this often is cheap.
        /// </summary>
        /// <returns>The connected controllers: the ones already open in the order they connected, then new ones in SDL order.</returns>
        public IEnumerable<IController> GetControllers()
        {
            EnsureInitialized();

            lock (_sync)
            {
                if (_isDisposed)
                {
                    return Array.Empty<IController>();
                }

                // HIDAPI (Joy-Cons, Switch Pro, 8BitDo in Switch mode) and DirectInput devices, which is
                // what most Bluetooth controllers are, are only discovered while SDL pumps events.
                RefreshDevices();
                long checkTicks = Environment.TickCount64;
                long? previousCheckMsAgo = checkTicks - _lastCheckTicks;
                _lastCheckTicks = checkTicks;
                Dictionary<int, uint> removalEvents = TakeJoystickEvents();

                int numberOfDevices = Math.Max(SDL_NumJoysticks(), 0);
                var present = new List<int>(numberOfDevices);
                for (int i = 0; i < numberOfDevices; i++)
                {
                    present.Add(SDL_JoystickGetDeviceInstanceID(i));
                }

                bool firstEnumeration = _knownDeviceIds is null;
                SdlDeviceChange change = SdlDeviceSet.Diff(_knownDeviceIds ?? [], present);
                if (!firstEnumeration && !change.HasChanges)
                {
                    return _controllers.ToList();
                }

                _knownDeviceIds = [.. present];
                LogRemovals(change.Removed, removalEvents, previousCheckMsAgo);
                CloseDisconnected(change.Removed);

                if (numberOfDevices < 1)
                {
                    LogNoControllers();
                    return Array.Empty<IController>();
                }

                LogEnumerating(numberOfDevices);
                var added = change.Added.ToHashSet();

                for (int i = 0; i < numberOfDevices; i++)
                {
                    if (!added.Contains(present[i]))
                    {
                        continue;
                    }

                    SdlDeviceInfo info = DescribeDevice(i);
                    bool include = SdlDeviceDiagnostics.ShouldInclude(info, out string? skipReason);
                    string deviceLine = SdlDeviceDiagnostics.Format(info, skipReason);
                    LogDevice(deviceLine);

                    if (!include)
                    {
                        continue;
                    }

                    IntPtr controllerHandle = SDL_GameControllerOpen(i);

                    if (controllerHandle == IntPtr.Zero)
                    {
                        LogOpenFailed(i, SDL_GetError());
                        continue;
                    }

                    var controller = new SDL2GameController(controllerHandle, info.Vendor, info.Product);
                    _controllers.Add(controller);
                    _controllersByInstanceId[present[i]] = controller;
                    LogControllerConnected(controller.Name);
                }

                // Return a snapshot so later calls cannot mutate what callers are enumerating.
                return _controllers.ToList();
            }
        }

        /// <summary>
        /// Reads data from the specified controller.
        /// </summary>
        /// <param name="controller">The controller to read data from.</param>
        /// <returns>A <see cref="ControllerData"/> object containing the controller's state.</returns>
        public ControllerData? ReadData(IController controller)
        {
            EnsureInitialized();

            if (controller == null)
            {
                LogNullController();
                throw new ArgumentNullException(nameof(controller));
            }

            return controller.ReadData();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Initializes SDL2 subsystems.
        /// </summary>
        private void InitializeSDL()
        {
            // Hints only take effect if they are set before SDL_Init.
            // Use the button labels native to the controller.
            SDL_SetHint(SDL_HINT_GAMECONTROLLER_USE_BUTTON_LABELS, "1");
            SDL_SetHint(SDL_HINT_JOYSTICK_HIDAPI_JOY_CONS, "1");

            // Keep receiving controller input while Zwift or another window has focus.
            SDL_SetHint(SDL_HINT_JOYSTICK_ALLOW_BACKGROUND_EVENTS, "1");

            if (SDL_Init(SDL_INIT_VIDEO | SDL_INIT_JOYSTICK | SDL_INIT_GAMECONTROLLER | SDL_INIT_HAPTIC) < 0)
            {
                string sdlError = SDL_GetError();
                LogInitFailed(sdlError);
                string errorMessage = $"SDL could not initialize! Error: {sdlError}";
                throw new InvalidOperationException(errorMessage);
            }

            _isInitialized = true;
            LogInitialized();
            string linkedVersion = GetLinkedVersion();
            string compiledVersion = GetCompiledVersion();
            LogVersions(linkedVersion, compiledVersion);
        }

        /// <summary>
        /// Lets SDL discover devices that appear or change since the last call.
        /// </summary>
        private static void RefreshDevices()
        {
            SDL_PumpEvents();
            SDL_JoystickUpdate();
        }

        /// <summary>
        /// Takes the joystick and game controller events out of the SDL event queue and returns, for each device removal
        /// among them, the SDL tick count at which SDL queued it. Nothing else reads this queue, so draining it keeps it
        /// from filling up, which would make SDL drop new events.
        /// </summary>
        private static Dictionary<int, uint> TakeJoystickEvents()
        {
            var removals = new Dictionary<int, uint>();
            var events = new SDL_Event[64];
            int count;
            do
            {
                count = SDL_PeepEvents(events, events.Length, SDL_eventaction.SDL_GETEVENT, FirstJoystickEvent, LastJoystickEvent);
                for (int i = 0; i < count; i++)
                {
                    if (events[i].type == SDL_EventType.SDL_JOYDEVICEREMOVED)
                    {
                        removals[events[i].jdevice.which] = events[i].jdevice.timestamp;
                    }
                }
            }
            while (count == events.Length);

            return removals;
        }

        /// <summary>
        /// Logs, at debug level, when each removal was detected and how long before that SDL reported it.
        /// </summary>
        private void LogRemovals(IReadOnlyList<int> instanceIds, Dictionary<int, uint> removalEvents, long? previousCheckMsAgo)
        {
            if (instanceIds.Count == 0 || !_logger.IsEnabled(LogLevel.Debug))
            {
                return;
            }

            DateTime detectedAt = DateTime.Now;
            uint now = SDL_GetTicks();
            foreach (int instanceId in instanceIds)
            {
                long? sdlReportedMsAgo = removalEvents.TryGetValue(instanceId, out uint queuedAt) ? unchecked(now - queuedAt) : null;
                string line = SdlDeviceDiagnostics.FormatRemoval(instanceId, detectedAt, sdlReportedMsAgo, previousCheckMsAgo);
                LogRemovalDetected(line);
            }
        }

        private static string GetLinkedVersion()
        {
            SDL_GetVersion(out SDL_version linked);
            return $"{linked.major}.{linked.minor}.{linked.patch} ({SDL_GetRevision()})";
        }

        private static string GetCompiledVersion()
        {
            return $"{SDL_MAJOR_VERSION}.{SDL_MINOR_VERSION}.{SDL_PATCHLEVEL}";
        }

        /// <summary>
        /// Reads what SDL knows about the device at the given index.
        /// </summary>
        internal static SdlDeviceInfo DescribeDevice(int index)
        {
            return new SdlDeviceInfo(
                index,
                SDL_JoystickNameForIndex(index),
                SDL_JoystickGetDeviceGUID(index).ToByteArray(),
                SDL_JoystickGetDeviceVendor(index),
                SDL_JoystickGetDeviceProduct(index),
                SDL_JoystickGetDeviceType(index).ToString(),
                SDL_IsGameController(index) == SDL_bool.SDL_TRUE);
        }

        /// <summary>
        /// Ensures that SDL2 is initialized before performing operations.
        /// </summary>
        private void EnsureInitialized()
        {
            if (!_isInitialized)
            {
                LogNotInitialized();
                throw new InvalidOperationException("SDL2 has not been initialized.");
            }
        }

        /// <summary>
        /// Closes the controllers whose devices disconnected.
        /// </summary>
        /// <param name="instanceIds">The SDL instance ids of the disconnected devices.</param>
        private void CloseDisconnected(IReadOnlyList<int> instanceIds)
        {
            foreach (int instanceId in instanceIds)
            {
                // Skipped devices (plain joysticks, failed opens) were never opened, so there is nothing to close.
                if (!_controllersByInstanceId.Remove(instanceId, out IController? controller))
                {
                    continue;
                }

                _controllers.Remove(controller);
                controller.Dispose();
                LogControllerDisconnected(controller.Name);
            }
        }

        /// <summary>
        /// Disposes of all currently tracked controllers.
        /// </summary>
        private void DisposeControllers()
        {
            foreach (var controller in _controllers)
            {
                controller.Dispose();
                LogControllerDisposed();
            }

            _controllers.Clear();
            _controllersByInstanceId.Clear();
            _knownDeviceIds = null;
        }

        #endregion

        #region Logging

        [LoggerMessage(Level = LogLevel.Information, Message = "No game controllers found.")]
        private partial void LogNoControllers();

        [LoggerMessage(Level = LogLevel.Information, Message = "SDL2 linked version {Linked}, compiled against {Compiled}.")]
        private partial void LogVersions(string linked, string compiled);

        [LoggerMessage(Level = LogLevel.Information, Message = "SDL2 reports {Count} joystick device(s).")]
        private partial void LogEnumerating(int count);

        [LoggerMessage(Level = LogLevel.Information, Message = "{Line}")]
        private partial void LogDevice(string line);

        [LoggerMessage(Level = LogLevel.Error, Message = "Failed to open controller at index {Index}: {Error}")]
        private partial void LogOpenFailed(int index, string error);

        [LoggerMessage(Level = LogLevel.Information, Message = "Controller connected: {Name}")]
        private partial void LogControllerConnected(string name);

        [LoggerMessage(Level = LogLevel.Information, Message = "Controller disconnected: {Name}")]
        private partial void LogControllerDisconnected(string name);

        [LoggerMessage(Level = LogLevel.Debug, Message = "{Line}")]
        private partial void LogRemovalDetected(string line);

        [LoggerMessage(Level = LogLevel.Error, Message = "Attempted to read data from a null controller.")]
        private partial void LogNullController();

        [LoggerMessage(Level = LogLevel.Error, Message = "SDL could not initialize! Error: {Error}")]
        private partial void LogInitFailed(string error);

        [LoggerMessage(Level = LogLevel.Information, Message = "SDL2 initialized successfully.")]
        private partial void LogInitialized();

        [LoggerMessage(Level = LogLevel.Error, Message = "SDL2 has not been initialized.")]
        private partial void LogNotInitialized();

        [LoggerMessage(Level = LogLevel.Information, Message = "Disposed a controller.")]
        private partial void LogControllerDisposed();

        [LoggerMessage(Level = LogLevel.Information, Message = "SDL2InputService disposed and SDL2 resources released.")]
        private partial void LogDisposed();

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Releases SDL2 resources and disposes of controllers.
        /// </summary>
        public void Dispose()
        {
            if (!_isDisposed)
            {
                lock (_sync)
                {
                    DisposeControllers();
                    SDL_Quit();
                    _isDisposed = true;
                }
                LogDisposed();
            }
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}