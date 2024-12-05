namespace GameControllerForZwift.Core
{
    public class InputStateChangedEventArgs : EventArgs
    {
        public ControllerInput Input { get; }
        public ControllerData Data { get; }
        public bool IsPressed => Data.IsPressed(Input);

        public InputStateChangedEventArgs(ControllerInput input, ControllerData data)
        {
            Input = input;
            Data = data;
        }
    }
}
