namespace GameControllerForZwift.Core.Controller
{
    public class InputStateChangedEventArgs : EventArgs
    {
        public ControllerData Data { get; }

        public InputStateChangedEventArgs(ControllerData data)
        {
            Data = data;
        }
    }
}
