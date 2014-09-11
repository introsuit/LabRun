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
using System.ComponentModel;

namespace UserControls
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class TabControl : UserControl, ControlUnit, INotifyPropertyChanged
    {
        private MainUI parent = null;
        private TestApp testApp = null;
        private Service service = Service.getInstance();
        private bool inited = false;
        private bool clientSelected = false;
        private ZtreeControl ztreeCtrl = null;
        public TabItem TabItem { get; set; }

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
                btnDelResults.IsEnabled = true;
                btnGetResults.IsEnabled = true;
            }
        }

        private bool active;
        public bool Active
        {
            get { return active; }
            set
            {
                if (value != active)
                {
                    active = value;
                    OnPropertyChanged("Active");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string p)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(p));
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
                SetClickable();
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

            parent.SetTabActivity(TabItem, computerNames, true);
            parent.updateStatus("In Progress...");

            if (testApp is ZTree)
            {
                //MessageBox.Show("Make sure", "Attention", MessageBoxButton.OK, MessageBoxImage.Information);
                WindowSize winSize = ztreeCtrl.GetSelectedWindowSize();
                ((ZTree)testApp).TransferAndRun(computerNames, winSize);
            }
            else
            {
                bool copyAll = (bool)cbxCopyAll.IsChecked;
                testApp.TransferAndRun(computerNames, copyAll);
            }
        }

        private void btnKill_Click(object sender, RoutedEventArgs e)
        {
            List<LabClient> clients = parent.getSelectedClients();

            parent.updateStatus("In Progress...");
            parent.SetTabActivity(TabItem, clients, false);

            string appExeName = testApp.AppExeName;
            foreach (LabClient client in clients)
            {
                try
                {
                    service.killRemoteProcess(client.ComputerName, appExeName);
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
                testApp.TransferResults(parent.getSelectedClients());
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

        private void SetClickable()
        {
            btnKill.IsEnabled = clientSelected;
            btnRun.IsEnabled = clientSelected && inited;
            btnGetResults.IsEnabled = clientSelected;
            btnDelResults.IsEnabled = clientSelected;
        }

        public void ButtonClickable(bool enabled)
        {
            clientSelected = enabled;
            if (testApp is ZTree)
            {
                clientSelected = true;
            }
            //btnBrowse.IsEnabled = enabled;
            SetClickable();
        }

        private void btnDelResults_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure you want to delete all result files from lab computers?\nMake sure you have a backup!", "Are you sure?", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                if (testApp is ZTree)
                {
                    MessageBoxResult check = MessageBox.Show("Make sure that ZTree admin application is not running!", "Are you sure?", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                testApp.DeleteResults(parent.getSelectedClients());
            }
        }

        public void SetProject(string projectName)
        {
            testApp.ProjectName = projectName;
        }

        private void btnOpenRes_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                testApp.OpenResultsFolder();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
