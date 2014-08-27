using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceLibrary
{
    public class PsychoPy : TestApp
    {
        public PsychoPy()
            : base("PsychoPy", "python.exe")
        {
            Extension = "py";
            ExtensionDescription = "Python files (*.py)|*.py|PsychoPy Test Files (*.psyexp)|*.psyexp";

            resultExts.Add("psydat");
            resultExts.Add("csv");
            resultExts.Add("log");
        }
    }
}
