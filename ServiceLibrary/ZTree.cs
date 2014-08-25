using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceLibrary
{
    public class ZTree : TestApp
    {
        public ZTree() : base("ZTree", @"Path to ZTree...") 
        {
            Extension = "ztree";
            ExtensionDescription = "ZTree Test Files (*.ztree)|*.ztree";

            resultExts = new string[3];
            resultExts[0] = "psydat";
            resultExts[1] = "csv";
            resultExts[2] = "log";
        }
    }
}
