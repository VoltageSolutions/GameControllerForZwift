using GameControllerForZwift.Core.Controller;

namespace GameControllerForZwift.Core
{
    public interface IInputService
    {
        public IEnumerable<IController> GetControllers();
        public ControllerData ReadData(IController controller);
    }
}
