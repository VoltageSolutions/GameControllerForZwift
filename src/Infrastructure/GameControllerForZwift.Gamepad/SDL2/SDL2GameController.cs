using GameControllerForZwift.Core.Controller;
using static SDL2.SDL;

namespace GameControllerForZwift.Gamepad.SDL2
{
    public class SDL2GameController : IController, IDisposable
    {
        #region Fields

        private readonly IntPtr _handle;
        private readonly string _vendorProduct;
        private bool _isDisposed;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="SDL2GameController"/> class.
        /// </summary>
        /// <param name="handle">The SDL2 game controller handle.</param>
        public SDL2GameController(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
            {
                throw new ArgumentException("Invalid SDL2 controller handle.", nameof(handle));
            }

            _handle = handle;

            ushort vendor = SDL_GameControllerGetVendor(handle);
            ushort product = SDL_GameControllerGetProduct(handle);
            _vendorProduct = $"{vendor:X4}-{product:X4}";

            Name = SDL_GameControllerName(handle) ?? "Unknown Controller";
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the name of the game controller.
        /// </summary>
        public string Name { get; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Reads the current state of the game controller and returns a <see cref="ControllerData"/> object.
        /// </summary>
        /// <returns>A <see cref="ControllerData"/> object containing the current controller state.</returns>
        public ControllerData ReadData()
        {
            if (_isDisposed)
            {
                return null;
            }

            SDL_GameControllerUpdate();

            return new ControllerData
            {
                A = GetButtonState(SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_A),
                B = GetButtonState(SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_B),
                X = GetButtonState(SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_X),
                Y = GetButtonState(SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_Y),

                LeftBumper = GetButtonState(SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSHOULDER),
                RightBumper = GetButtonState(SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSHOULDER),
                View = GetButtonState(SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_BACK),
                Menu = GetButtonState(SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_START),

                LeftThumbstick_Click = GetButtonState(SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSTICK),
                RightThumbstick_Click = GetButtonState(SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSTICK),

                DPad_Up = GetButtonState(SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_UP),
                DPad_Down = GetButtonState(SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_DOWN),
                DPad_Left = GetButtonState(SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_LEFT),
                DPad_Right = GetButtonState(SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_RIGHT),

                LeftThumbstickX = GetAxisValue(SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTX),
                LeftThumbstickY = GetAxisValue(SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTY),
                RightThumbstickX = GetAxisValue(SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTX),
                RightThumbstickY = GetAxisValue(SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTY),
                LeftTrigger = GetAxisValue(SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERLEFT),
                RightTrigger = GetAxisValue(SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERRIGHT),

                Timestamp = DateTime.Now
            };
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Gets the state of a specified button.
        /// </summary>
        /// <param name="button">The button to check.</param>
        /// <returns><c>true</c> if the button is pressed; otherwise, <c>false</c>.</returns>
        private bool GetButtonState(SDL_GameControllerButton button)
        {
            return SDL_GameControllerGetButton(_handle, button) == 1;
        }

        /// <summary>
        /// Gets the value of a specified axis.
        /// </summary>
        /// <param name="axis">The axis to check.</param>
        /// <returns>The axis value.</returns>
        private int GetAxisValue(SDL_GameControllerAxis axis)
        {
            return SDL_GameControllerGetAxis(_handle, axis);
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Releases unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (!_isDisposed)
            {
                if (_handle != IntPtr.Zero)
                {
                    SDL_GameControllerClose(_handle);
                }
                _isDisposed = true;
            }
        }

        #endregion
    }
}