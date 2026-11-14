using GameControllerForZwift.Core.Controller;
using GameControllerForZwift.Core;
using Microsoft.Extensions.Logging;
using static SDL2.SDL;

namespace GameControllerForZwift.Gamepad.SDL2
{
    public sealed partial class SDL2InputService : IInputService, IDisposable
    {
        #region Fields

        private readonly ILogger<SDL2InputService> _logger;
        private readonly List<IController> _controllers = new();
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
        /// Gets the currently connected game controllers.
        /// </summary>
        /// <returns>A collection of <see cref="IController"/> instances.</returns>
        public IEnumerable<IController> GetControllers()
        {
            EnsureInitialized();

            lock (_sync)
            {
                // Dispose of any existing controllers to avoid orphans.
                DisposeControllers();

                // HIDAPI (Joy-Cons, Switch Pro, 8BitDo in Switch mode) and DirectInput devices, which is
                // what most Bluetooth controllers are, are only discovered while SDL pumps events.
                RefreshDevices();

                int numberOfDevices = SDL_NumJoysticks();

                if (numberOfDevices < 1)
                {
                    LogNoControllers();
                    return Array.Empty<IController>();
                }

                LogEnumerating(numberOfDevices);

                for (int i = 0; i < numberOfDevices; i++)
                {
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

                    var controller = new SDL2GameController(controllerHandle);
                    _controllers.Add(controller);
                    LogControllerConnected(controller.Name);
                }

                // Return a snapshot so later refreshes cannot mutate what callers are enumerating.
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
        private static SdlDeviceInfo DescribeDevice(int index)
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