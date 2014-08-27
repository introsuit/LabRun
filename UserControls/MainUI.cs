using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceLibrary;

namespace UserControls
{
    public interface MainUI
    {
        void updateStatus(string msg);
        List<string> getSelectedClientsNames();
        List<LabClient> getSelectedClients();
    }
}
