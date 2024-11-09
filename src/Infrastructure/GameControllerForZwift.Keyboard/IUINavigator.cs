using GameControllerForZwift.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameControllerForZwift.Keyboard
{
    public interface IUINavigator
    {
        public ActionResult PerformAction(ZwiftFunction zwiftFunction, ZwiftPlayerView playerView, ZwiftRiderAction riderAction);
    }
}
