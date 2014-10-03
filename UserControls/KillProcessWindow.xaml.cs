using ServiceLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace UserControls
{
    /// <summary>
    /// Interaction logic for KillProcessWindow.xaml
    /// </summary>
    public partial class KillProcessWindow : Window
    {
        private CustomRun parent = null;
        private Service service = Service.getInstance();
        private HashSet<string> procList;
        public KillProcessWindow(CustomRun parent)
        
        {
            InitializeComponent();
            this.parent = parent;
            List<string> allProcList = new List<string>();
            foreach (LabClient client in parent.getParent().getSelectedClients())
            {
                foreach (CompAndProcesses temp in parent.procList) { 
                    if (temp.computer == client)
                    allProcList.AddRange(temp.processes);
                }
            }
            procList = new HashSet<string>(allProcList);
            lstbxProcesses.ItemsSource = procList;
        }

        private void btnKill_Click(object sender, RoutedEventArgs e)
        {
            btnKill.IsEnabled = true;
            string exename = (string)lstbxProcesses.SelectedValue;
            service.CloseCustomProcess(parent.getParent().getSelectedClients(), exename);
            procList.Remove(exename);
            lstbxProcesses.ItemsSource = null;
            lstbxProcesses.ItemsSource = procList;
            parent.ProcessStopped(exename);

        }

        private void lstbxProcesses_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            btnKill.IsEnabled = true;
        }
    }
}
