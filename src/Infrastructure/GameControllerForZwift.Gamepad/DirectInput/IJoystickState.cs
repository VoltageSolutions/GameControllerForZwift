namespace GameControllerForZwift.Gamepad.DirectInput
{
    public interface IJoystickState
    {
        bool[] Buttons { get; }
        int[] PointOfViewControllers { get; }
        int X { get; }
        int Y { get; }
        int Z { get; }
        int RotationX { get; }
        int RotationY { get; }
        int RotationZ { get; }
    }
}
