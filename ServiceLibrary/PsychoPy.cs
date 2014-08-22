using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceLibrary
{
    public class PsychoPy : TestApp
    {
        public PsychoPy()
        {
            this.applicationName = "PsychoPy";
            this.applicationExecutableName = "python";
        }
    }
}
