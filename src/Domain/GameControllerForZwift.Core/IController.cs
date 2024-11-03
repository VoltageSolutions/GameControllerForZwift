using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameControllerForZwift.Core
{
    public interface IController
    {
        public ControllerSpecs? Specifications { get; }
    }
}
