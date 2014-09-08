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
    public partial class TabControl : UserControl, ControlUnit
    {
        private MainUI parent = null;
        private TestApp testApp = null;
        private Service service = Service.getInstance();
        private bool inited = false;
        private String project = "UnknownProject";
        private bool clientSelected = false;
        private ZtreeControl ztreeCtrl = null;

        public TabControl(MainUI parent, TestApp testApp)
        {
            InitializeComponent();
            this.parent = parent;
            this.testApp = testApp;

            if (testApp is ZTree)
            {
                inited = true;

                ztreeCtrl = new ZtreeControl((ZTree)testApp);
                ztreeControls.Children.Add(ztreeCtrl);
            }
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

                lblBrowse.Content = testFullPath;
                inited = true;

                btnRun.IsEnabled = true;
                testApp.Initialize(testFullPath);
                ButtonClickable(true);
            }
        }

        public void setTestLogo(string path)
        {
            var uriSource = new Uri(path, UriKind.Relative);
            imgTest.Source = new BitmapImage(uriSource);
        }

        private void btnRun_Click(object sender, RoutedEventArgs e)
        {
            List<LabClient> computerNames = parent.getSelectedClients();

            if (computerNames.Count == 0)
            {
                MessageBox.Show("Select some clients first!");
                return;
            }

            parent.updateStatus("In Progress...");
            if (testApp is ZTree)
            {
                WindowSize winSize = ztreeCtrl.GetSelectedWindowSize();
                ((ZTree)testApp).TransferAndRun(computerNames, project, winSize);
            }
            else
            {
                testApp.TransferAndRun(computerNames, project);
            }
        }

        private void btnKill_Click(object sender, RoutedEventArgs e)
        {
            parent.updateStatus("In Progress...");
            string appExeName = testApp.AppExeName;
            foreach (string computerName in parent.getSelectedClientsNames())
            {
                try
                {
                    service.killRemoteProcess(computerName, appExeName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void btnGetResults_Click(object sender, RoutedEventArgs e)
        {
            parent.updateStatus("In Progress...");

            try
            {
                testApp.TransferResults(parent.getSelectedClients(), project);
            }
            catch (TimeoutException ex)
            {
                MessageBox.Show("Transfer timed out: " + ex.Message, "Transfer Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                parent.updateStatus("Idle");
                return;
            }
        }

        private void cmbWindowSizes_Loaded(object sender, RoutedEventArgs e)
        {
            //cmbWindowSizes.ItemsSource = Enum.GetValues(typeof(WindowSize)).Cast<WindowSize>();
        }

        public void ButtonClickable(bool enabled)
        {
            clientSelected = enabled;
            //btnBrowse.IsEnabled = enabled;
            btnKill.IsEnabled = clientSelected;
            btnRun.IsEnabled = clientSelected && inited;
            btnGetResults.IsEnabled = clientSelected && inited;
            btnDelResults.IsEnabled = clientSelected && inited;
        }

        private void btnDelResults_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure you want to delete all result files from lab computers?\nMake sure you have a backup!", "Are you sure?", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                testApp.DeleteResults(parent.getSelectedClients(), project);
            }
        }

        public void SetProject(string projectName)
        {
            project = projectName;
        }
    }
}
