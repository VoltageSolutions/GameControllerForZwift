using SharpDX.DirectInput;

namespace GameControllerForZwift.Gamepad.DirectInput
{
    public class JoystickStateWrapper : IJoystickState
    {
        #region Fields
        private readonly JoystickState _state;
        #endregion
        #region Constructor
        public JoystickStateWrapper(JoystickState state) => _state = state;
        #endregion
        #region Properties
        public bool[] Buttons => _state.Buttons;
        public int[] PointOfViewControllers => _state.PointOfViewControllers;
        public int X => _state.X;
        public int Y => _state.Y;
        public int RotationX => _state.RotationX;
        public int RotationY => _state.RotationY;
        #endregion
    }
}
