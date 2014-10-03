using ServiceLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
    /// Interaction logic for CustomRun.xaml
    /// </summary>
    public partial class CustomRun : UserControl, ControlUnit
    {
        private MainUI parent = null;
        private Service service = Service.getInstance();
        private CustomRunTestApp crTestApp;
        public List<CompAndProcesses> procList { get; set; }
        public string filePath { get; set; }
        public string fileName { get; set; }
        public string DirPath { get; set; }
        public string DirFileName { get; set; }
        public string DirFileNameWithExtraDir { get; set; }
        public Boolean isEnabledSingle { get; set; }
        public Boolean isEnabledDir { get; set; }
        public Boolean IsEnabledDirTransfernRun { get; set; }
        public List<string> extensions { get; set; }
        public string TimeStamp { get; set; }
        public string Parameter { get; set; }
        public TabItem TabItem { get; set; }


        public CustomRun(MainUI parent, List<LabClient> clients)
        {
            InitializeComponent();
            isEnabledSingle = false;
            isEnabledDir = false;
            this.parent = parent;
            this.TimeStamp = service.GetCurrentTimestamp();
            this.lblTimestmp.Content = "Timestamp: " + this.TimeStamp;
            this.extensions = new List<string>();
            InitProcList(clients);
        }

        public void InitProcList(List<LabClient> clients)
        {
            this.procList = new List<CompAndProcesses>();
            foreach (LabClient client in clients)
            {
                CompAndProcesses ComProc = new CompAndProcesses();
                ComProc.computer = client;
                ComProc.processes = new List<string>();
                this.procList.Add(ComProc);
            }
        }

        public MainUI getParent() {
            return this.parent;
        }

        public void ButtonClickable(bool enabled)
        {
            btnCleanCustomDir.IsEnabled = enabled;
            btnGetResults.IsEnabled = enabled;
            if ((IsEnabledDirTransfernRun) && (enabled))
            {
                btnTransfernRunDir.IsEnabled = true;
            }
            if ((isEnabledSingle) && (enabled))
            {
                btnTransferSingleFile.IsEnabled = true;
                btnTransfernRunSingleFile.IsEnabled = true;
            }
            if ((isEnabledDir) && (enabled))
            {

                btnTransferDir.IsEnabled = true;
                btnBrowseDirFileToRun.IsEnabled = true;
            }
            if ((isEnabledSingle == false) || (enabled == false))
            {
                btnTransferSingleFile.IsEnabled = false;
                btnTransfernRunSingleFile.IsEnabled = false;
            }
            if ((isEnabledDir == false) || (enabled == false))
            {
                btnTransfernRunDir.IsEnabled = false;
                btnTransferDir.IsEnabled = false;
                btnBrowseDirFileToRun.IsEnabled = false;
            }
        }

        public void SetProject(string projectName)
        {
            if (crTestApp != null)
            {
                crTestApp.ProjectName = projectName;
            }
        }

        private void btnTimestmp_Click(object sender, RoutedEventArgs e)
        {
            this.TimeStamp = service.GetCurrentTimestamp();
            this.lblTimestmp.Content = "Timestamp: " + this.TimeStamp;
        }

        private void btnSetParameter_Click(object sender, RoutedEventArgs e)
        {
            this.Parameter = txtParameter.Text;
            this.lblParameter.Content = "Current parameter: " + this.Parameter;
        }

        private void btnBrowseSingleFile_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file name and save the path 
            if (result == true)
            {
                this.filePath = dlg.FileName;
                string[] words = this.filePath.Split('\\');
                foreach (string word in words)
                {
                    this.fileName = word;
                }
                this.isEnabledSingle = true;
                if (parent.getSelectedClients().Count != 0)
                {
                    btnTransfernRunSingleFile.IsEnabled = true;
                    btnTransferSingleFile.IsEnabled = true;
                }

                lblFilePath.Content = this.filePath;
            }
        }


        private void btnTransferSingleFile_Click(object sender, RoutedEventArgs e)
        {
            List<LabClient> clients = parent.getSelectedClients();
            ThreadStart tssingle = delegate()
            {
                
                Service.getInstance().CopyFilesToNetworkShare(this.filePath, this.TimeStamp);
                Service.getInstance().CopyFilesFromNetworkShareToClients(this.filePath, this.fileName, clients, this.TimeStamp);
            };
            service.RunInNewThread(tssingle);
            
        }

        private void btnTransfernRunSingleFile_Click(object sender, RoutedEventArgs e)
        {
            // Check file name and extension for keeping track of running processes
            string filename = "";
            string[] split = this.filePath.Split('\\');
            foreach (string temp in split)
            {
                filename = temp;
            }
            string extname = "";
            string[] extSplit = this.filePath.Split('.');
            foreach (string temp in extSplit)
            {
                extname = temp;
            }

            // If launched file is an exe, add it to the process list
            if (extname == "exe")
            {

                    foreach (LabClient client in parent.getSelectedClients())
                    {
                        foreach (CompAndProcesses proc_client in this.procList)
                        {
                            if (client.Equals(proc_client.computer))
                            {
                                {proc_client.processes.Add(filename); }
                            }
                        }
                    }
            }

            // Check if param is not null
            string param = "";
            if (this.Parameter != null)
                param = this.Parameter;

            // Set Custom tab as "Running"
            List<LabClient> clients = parent.getSelectedClients();
            parent.SetTabActivity(TabItem, clients, true);

            // Start Custom Running in new thread
            ThreadStart ts = delegate()
            {
                Service.getInstance().CopyFilesToNetworkShare(this.filePath, this.TimeStamp);
                Service.getInstance().CopyAndRunFilesFromNetworkShareToClients(this.filePath, this.fileName, clients, param, this.TimeStamp);
            };
            service.RunInNewThread(ts);

        }

        private void btnBrowseDir_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog browse = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = browse.ShowDialog();

            if (browse.SelectedPath != "")
            {
                this.DirPath = browse.SelectedPath;
                lblDirPath.Content = "Folder: " + this.DirPath;
                this.isEnabledDir = true;
                if (parent.getSelectedClients().Count != 0)
                {
                    btnBrowseDirFileToRun.IsEnabled = true;
                    btnTransferDir.IsEnabled = true;
                    btnBrowseDirFileToRun.IsEnabled = true;
                    this.isEnabledDir = true;
                }
            }
        }

        private void btnBrowseDirFileToRun_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.InitialDirectory = this.DirPath;

            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file name and save the path 
            if (result == true)
            {
                string pathChosenFile = "";
                this.DirFileName = dlg.FileName;

                string[] directory = this.DirPath.Split('\\');
                string[] file = this.DirFileName.Split('\\');
                int i = 0;
                string subFileName = "";
                foreach (string word in file)
                {
                    if (i > directory.Length - 1)
                    {
                        subFileName += "\\" + file[i];
                    }
                    i++;
                }
                lblDirFilePath.Content = "File: " + subFileName;
                this.DirFileNameWithExtraDir = subFileName;
                this.isEnabledDir = true;
                if (parent.getSelectedClients().Count != 0)
                {
                    this.IsEnabledDirTransfernRun = true;
                    this.btnTransfernRunDir.IsEnabled = true;
                    this.btnTransferDir.IsEnabled = true;
                }

            }
        }

        private void btnTransfernRunDir_Click(object sender, RoutedEventArgs e)
        {
            string param = "";
            if (this.Parameter != null)
                param = this.Parameter;
            List<LabClient> clients = parent.getSelectedClients();
            parent.SetTabActivity(TabItem, clients, true);

            ThreadStart ts = delegate()
            {
                Service.getInstance().CopyAndRunFolder(clients, this.DirPath, this.DirFileNameWithExtraDir, param, this.TimeStamp);
            };
            service.RunInNewThread(ts);


        }

        private void btnDefineExtensions_Click(object sender, RoutedEventArgs e)
        {
            Window WindowResultsExtensions = new ResultsExtensionWindow(this, this.extensions);
            WindowResultsExtensions.Show();
        }

        private void btnGetResults_Click(object sender, RoutedEventArgs e)
        {
            if (extensions.Count != 0)
            {
                crTestApp = new CustomRunTestApp(this.extensions);
                this.SetProject(parent.getProject());
                crTestApp.testFolder = @"C:\Cobe Lab\";
                crTestApp.TransferResults(this.parent.getSelectedClients());
            }
            else
                MessageBox.Show("Define some extensions to retrieve!");
        }

        private void btnCleanCustomDir_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(@"Are you sure you want to delete the entire ""Transferred files"" directory? Verify that result files are backed up and that nothing of value remains in the directory! Do you wish to continue?", "Are you sure?", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                service.deleteFiles(parent.getSelectedClients());
            }
        }

        private void btnTransferToDMS_Click(object sender, RoutedEventArgs e)
        {
            if (service.User == null)
            {
                MessageBox.Show(@"You must be logged in to do that.", "Login required", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            MessageBoxResult result = MessageBox.Show("All results data from your project will be copied to DMS\nTo see what will be transferred, click \"Open Results\" to view your project folder.\n\nAre you sure you want to continue?", "Are you sure?", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    if (crTestApp == null)
                    {
                        crTestApp = new CustomRunTestApp(this.extensions);
                        this.SetProject(parent.getProject());
                        crTestApp.testFolder = service.TestFolder;
                    }
                    crTestApp.ToDms();
                }
                catch (DirectoryNotFoundException ex)
                {
                    MessageBox.Show("Project folder: \"" + ex.Message + "\" was not found! Make sure that you have transferred results to this project.", "Directory Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnTransferDir_Click(object sender, RoutedEventArgs e)
        {

            List<LabClient> clients = parent.getSelectedClients();
            service.CopyFolder(clients, this.DirPath, this.TimeStamp);
        }

        private void btnKill_Click(object sender, RoutedEventArgs e)
        {
            Window KillProcessWindow = new KillProcessWindow(this);
            KillProcessWindow.Show();
        }

        public void ProcessStopped(string procName) {
            foreach (LabClient client in parent.getSelectedClients()) {
                foreach (CompAndProcesses compProc in this.procList) {
                    if (compProc.computer == client) {
                        compProc.processes.Remove(procName);
                    }
                }
            }
        }
    }
}
