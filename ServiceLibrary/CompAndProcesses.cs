﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceLibrary
{
    public class CompAndProcesses
    {
        public LabClient computer { get; set; }
        public HashSet<String> processes { get; set; }
    }
}
