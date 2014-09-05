﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UserControls
{
    public interface ControlUnit
    {
        void ButtonClickable(bool enabled);
        void SetProject(string projectName);
    }
}
