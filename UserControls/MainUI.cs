using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UserControls
{
    public interface MainUI
    {
        void updateStatus(string msg);
        List<string> getSelectedClients();
    }
}
