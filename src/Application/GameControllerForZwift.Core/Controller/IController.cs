namespace GameControllerForZwift.Core.Controller
{
    public interface IController : IDisposable
    {
        public string Name { get; }
        public ControllerData ReadData();
    }
}
