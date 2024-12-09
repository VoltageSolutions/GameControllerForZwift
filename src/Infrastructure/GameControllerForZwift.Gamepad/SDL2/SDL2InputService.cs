using GameControllerForZwift.Core;
using Microsoft.Extensions.Logging;
using static SDL2.SDL;

namespace GameControllerForZwift.Gamepad.SDL2
{
    public class SDL2InputService : IInputService
    {
        #region Fields

        private readonly ILogger<SDL2InputService> _logger;

        #endregion

        #region Constructor

        public SDL2InputService(ILogger<SDL2InputService> logger)
        {
            _logger = logger;


            if (SDL_Init(SDL_INIT_VIDEO | SDL_INIT_JOYSTICK | SDL_INIT_GAMECONTROLLER | SDL_INIT_HAPTIC) < 0)
            {
                _logger.LogError($"SDL could not initialize! Error: {SDL_GetError()}");
                return;
            }


            // Use the button labels native to the controller.
            SDL_SetHint(SDL_HINT_GAMECONTROLLER_USE_BUTTON_LABELS, "1");
            SDL_SetHint(SDL_HINT_JOYSTICK_HIDAPI_JOY_CONS, "1");

            // Potential future option - enable rumble
            //SDL_SetHint(SDL_HINT_JOYSTICK_HIDAPI_PS4_RUMBLE, "1");
            //SDL_SetHint(SDL_HINT_JOYSTICK_HIDAPI_PS5_RUMBLE, "1");
        }
        #endregion
        public IEnumerable<IController> GetControllers()
        {
            // how to dispose of previous orphaned objects when done?
            int numberOfGamepads = SDL_NumJoysticks();

            // If no controllers are connected, return an empty collection
            if (numberOfGamepads < 1)
            {
                return Enumerable.Empty<SDL2GameController>();
            }

            return Enumerable.Range(0, numberOfGamepads).Select(index =>
            {
                // Open the controller by index
                IntPtr controller = SDL_GameControllerOpen(index);

                // commenting this out means we don't handle failures
                //if (controller == IntPtr.Zero)
                //{
                //    _logger.LogError($"Unable to open controller at index {index}. Error: {SDL_GetError()}");
                //    return null;
                //}
                return new SDL2GameController(controller);
            });
        }

        public ControllerData ReadData(IController controller)
        {
            return controller.ReadData();
        }
    }
}
