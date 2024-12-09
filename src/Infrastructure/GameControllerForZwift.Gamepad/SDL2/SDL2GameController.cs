using GameControllerForZwift.Core;
using GameControllerForZwift.Gamepad.DirectInput.ControllerMapping;
using static SDL2.SDL;

namespace GameControllerForZwift.Gamepad.SDL2
{
    public class SDL2GameController : IController
    {
        #region Fields

        private readonly IntPtr _handle;
        private readonly string _vendorProduct;

        #endregion

        #region Constructor

        public SDL2GameController(IntPtr handle)
        {
            _handle = handle;

            ushort vendor = SDL_GameControllerGetVendor(handle);
            ushort product = SDL_GameControllerGetProduct(handle);
            _vendorProduct = $"{vendor:X4}-{product:X4}";

            // still need to consider device lookup to confirm name - maybe not a big a deal
            Name = SDL_GameControllerName(handle);
        }

        // destructor to close?

        #endregion
        
        #region Properties
        public string Name { get; }
        #endregion

        public ControllerData ReadData()
        {
            SDL_GameControllerUpdate();

            byte aButton = SDL_GameControllerGetButton(_handle, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_A);
            byte bButton = SDL_GameControllerGetButton(_handle, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_B);
            byte xButton = SDL_GameControllerGetButton(_handle, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_X);
            byte yButton = SDL_GameControllerGetButton(_handle, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_Y);

            byte dpadup = SDL_GameControllerGetButton(_handle, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_UP);
            byte dpaddown = SDL_GameControllerGetButton(_handle, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_DOWN);
            byte dpadleft = SDL_GameControllerGetButton(_handle, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_LEFT);
            byte dpadright = SDL_GameControllerGetButton(_handle, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_RIGHT);

            byte startButton = SDL_GameControllerGetButton(_handle, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_START);
            byte selectButton = SDL_GameControllerGetButton(_handle, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_BACK);
            byte leftBumper = SDL_GameControllerGetButton(_handle, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSHOULDER);
            byte rightBumper = SDL_GameControllerGetButton(_handle, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSHOULDER);

            byte leftThumbstick = SDL_GameControllerGetButton(_handle, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSTICK);
            byte rightThumbstick = SDL_GameControllerGetButton(_handle, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSTICK);

            //double leftStickX = Math.Round(SDL_GameControllerGetAxis(_handle, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTX) / 32767.0, 2);
            //double leftStickY = Math.Round(SDL_GameControllerGetAxis(_handle, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTY) / 32767.0, 2);
            //double rightStickX = Math.Round(SDL_GameControllerGetAxis(_handle, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTX) / 32767.0, 2);
            //double rightStickY = Math.Round(SDL_GameControllerGetAxis(_handle, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTY) / 32767.0, 2);

            int leftStickX = SDL_GameControllerGetAxis(_handle, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTX);
            int leftStickY = SDL_GameControllerGetAxis(_handle, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTY);
            int rightStickX = SDL_GameControllerGetAxis(_handle, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTX);
            int rightStickY = SDL_GameControllerGetAxis(_handle, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTY);


            int leftTrigger = SDL_GameControllerGetAxis(_handle, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERLEFT);
            int rightTrigger = SDL_GameControllerGetAxis(_handle, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERRIGHT);

            return new ControllerData
            {
                // Button mappings
                A = (aButton == 1) ? true : false,
                B = (bButton == 1) ? true : false,
                X = (xButton == 1) ? true : false,
                Y = (yButton == 1) ? true : false,

                LeftBumper = (leftBumper == 1) ? true : false,
                RightBumper = (rightBumper == 1) ? true : false,
                View = (selectButton == 1) ? true : false,
                Menu = (startButton == 1) ? true : false,

                LeftThumbstick = (leftThumbstick == 1) ? true : false,
                RightThumbstick = (rightThumbstick == 1) ? true : false,

                // Point of View (D-Pad) mappings
                DPadUp = (dpadup == 1) ? true : false,
                DPadRight = (dpaddown == 1) ? true : false,
                DPadDown = (dpadleft == 1) ? true : false,
                DPadLeft = (dpadright == 1) ? true : false,

                // Axis mappings
                LeftThumbstickX = leftStickX,
                LeftThumbstickY = leftStickY,
                RightThumbstickX = rightStickX,
                RightThumbstickY = rightStickY,
                LeftTrigger = leftTrigger,
                RightTrigger = rightTrigger,

                // Timestamp for when this data was captured
                Timestamp = DateTime.Now
            };
        }
    }
}
