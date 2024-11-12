using SharpDX.DirectInput;

namespace GameControllerForZwift.Gamepad.DirectInput
{
    public class JoystickStateWrapper : IJoystickState
    {
        private readonly JoystickState _state;

        public JoystickStateWrapper(JoystickState state) => _state = state;

        public bool[] Buttons => _state.Buttons;
        public int[] PointOfViewControllers => _state.PointOfViewControllers;
        public int X => _state.X;
        public int Y => _state.Y;
        public int RotationX => _state.RotationX;
        public int RotationY => _state.RotationY;
    }
}
