namespace GameControllerForZwift.Gamepad.DirectInput
{
    public interface IJoystick
    {
        void Acquire();
        void Poll();
        IJoystickState GetCurrentState();
    }
}
