using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceLibrary
{
    public class StatusEventArgs : EventArgs
    {
        public StatusEventArgs(string s)
        {
            msg = s;
        }
        private string msg;
        public string Message
        {
            get { return msg; }
        }
    }
}
