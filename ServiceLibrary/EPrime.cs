using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceLibrary
{
    public class EPrime : TestApp
    {
        public EPrime(string testFilePath) : base("EPrime", @"C:\Program Files (x86)\PST\E-Prime 2.0 Runtime (2.0.10.242)\Program\E-Run.exe", testFilePath) { }
    }
}
