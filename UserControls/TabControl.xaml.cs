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
using System.Reflection;

namespace UserControls
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class TabControl : UserControl
    {
        private MainUI parent = null;
        private TestApp testApp = null;
        private Service service = Service.getInstance();

        public TabControl(MainUI parent, TestApp testApp)
        {
            InitializeComponent();
            this.parent = parent;
            this.testApp = testApp;
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension 
            dlg.DefaultExt = testApp.Extension;
            dlg.Filter = testApp.ExtensionDescription;

            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                // Open document 
                string testFullPath = dlg.FileName;

                label1.Content = testFullPath;
                btnRun.IsEnabled = true;

                testApp.Initialize(testFullPath);
            }
        }

        public void setTestLogo(string path)
        {
            var uriSource = new Uri(path, UriKind.Relative);
            imgTest.Source = new BitmapImage(uriSource);
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
            string appExeName = testApp.AppExeName;
            foreach (string computerName in parent.getSelectedClients())
            {
                service.killRemoteProcess(computerName, appExeName);
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
