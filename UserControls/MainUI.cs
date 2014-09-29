using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceLibrary;
using System.Windows.Controls;

namespace UserControls
{
    public interface MainUI
    {
        void updateStatus(string msg);
        List<string> getSelectedClientsNames();
        List<LabClient> getSelectedClients();
        void SetProject(string projectName, bool checkForExistingProject);
        void SetTabActivity(TabItem tabItem, List<LabClient> selectedClients, bool active);
        void SetFeatureActivity(Feature feature, List<LabClient> selectedClients, bool active);
        string getProject();
    }

    public enum Feature
    {
        WEB, SHARESCR, INPUT
    }
}
