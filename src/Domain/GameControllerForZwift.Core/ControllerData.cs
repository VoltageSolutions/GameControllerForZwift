namespace GameControllerForZwift.Core
{
    public struct ControllerData
    {
        public bool A { get; set; }
        public bool B { get; set; }
        public bool X { get; set; }
        public bool Y { get; set; }
        public bool Menu { get; set; }
        public bool View { get; set; }
        public bool DPadUp { get; set; }
        public bool DPadDown { get; set; }
        public bool DPadLeft { get; set; }
        public bool DPadRight { get; set; }
        public bool LeftBumper { get; set; }
        public bool RightBumper { get; set; }
        public bool LeftThumbstick { get; set; }
        public bool RightThumbstick { get; set; }

        //public ControllerButtons Buttons;
        public double LeftThumbstickX { get; set; }
        public double LeftThumbstickY { get; set; }
        public double LeftTrigger { get; set; }
        public double RightThumbstickX { get; set; }
        public double RightThumbstickY { get; set; }
        public double RightTrigger { get; set; }
        public DateTime Timestamp { get; set; }
    }
}