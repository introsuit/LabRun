using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ServiceLibrary;

namespace UserControls
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class TabControl1 : UserControl
    {
        private MainUI parent = null;
        private TestApp testApp = null;
        private Type type;
        private Service service = Service.getInstance();

        public TabControl1(MainUI parent, Type type)
        {
            InitializeComponent();
            this.parent = parent;
            this.type = type;
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension 
            dlg.DefaultExt = ".py";
            dlg.Filter = "Python files (*.py)|*.py|PsychoPy Test Files (*.psyexp)|*.psyexp";

            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                // Open document 
                string testFullPath = dlg.FileName;

                label1.Content = testFullPath;
                btnRun.IsEnabled = true;

                //testApp = new PsychoPy(testFullPath);
                testApp = (TestApp)Activator.CreateInstance(type);
            }
        }

        private void btnRun_Click(object sender, RoutedEventArgs e)
        {
            List<string> computerNames = parent.getSelectedClients();

            if (computerNames.Count == 0)
            {
                MessageBox.Show("Select some clients first!");
                return;
            }

            parent.updateStatus("In Progress...");

            testApp.TransferAndRun(computerNames);
        }

        private void btnKill_Click(object sender, RoutedEventArgs e)
        {
            parent.updateStatus("In Progress...");
            foreach (string computerName in parent.getSelectedClients())
            {
                service.killRemoteProcess(computerName, "python.exe");
            }
        }

        private void btnGetResults_Click(object sender, RoutedEventArgs e)
        {
            parent.updateStatus("In Progress...");

            try
            {
                testApp.TransferResults(parent.getSelectedClients());
            }
            catch (TimeoutException ex)
            {
                MessageBox.Show("Transfer timed out: " + ex.Message, "Transfer Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                parent.updateStatus("Idle");
                return;
            }
        }
    }
}
