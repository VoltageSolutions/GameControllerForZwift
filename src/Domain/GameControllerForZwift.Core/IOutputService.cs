namespace GameControllerForZwift.Core
{
    public interface IOutputService
    {
        public ActionResult PerformAction(ZwiftFunction zwiftFunction, ZwiftPlayerView playerView, ZwiftRiderAction riderAction);
    }
}
