using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceLibrary
{
    public class EPrime : TestApp
    {
        public EPrime() : base("EPrime", @"C:\Program Files (x86)\PST\E-Prime 2.0 Runtime (2.0.10.242)\Program\E-Run.exe") 
        {
            Extension = "ebs2";
            ExtensionDescription = "E-Prime Test Files (*.ebs2)|*.ebs2";

            resultExts = new string[3];
            resultExts[0] = "edat2";
            resultExts[1] = "xml";
            resultExts[2] = "txt";
        }
    }
}
