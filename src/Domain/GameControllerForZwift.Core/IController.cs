namespace GameControllerForZwift.Core
{
    public interface IController
    {
        public string Name { get; }
        public ControllerData ReadData();
    }
}
