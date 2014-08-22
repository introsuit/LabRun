using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceLibrary
{
    public class PsychoPy : TestApp
    {
        public PsychoPy(string testFilePath) : base("PsychoPy", "python", testFilePath) { }
    }
}
