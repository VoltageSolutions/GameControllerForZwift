namespace GameControllerForZwift.Core
{
    public interface IController : IDisposable
    {
        public string Name { get; }
        public ControllerData ReadData();
    }
}
