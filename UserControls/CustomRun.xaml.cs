using ServiceLibrary;
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
        public string filePath { get; set; }
        public string fileName { get; set; }
        public string DirPath { get; set; }
        public string DirFileName { get; set; }
        public string DirFileNameWithExtraDir { get; set; }
        public Boolean isEnabled { get; set; }
        public CustomRun(MainUI parent)
        {
            InitializeComponent();
            
            isEnabled = false;
            this.parent = parent;
        }

        private void browse_Btn_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // OpSet filter for file extension and default file extension 
            //dlg.DefaultExt = testApp.Extension;
            //dlg.Filter = testApp.ExtensionDescription;

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
                this.isEnabled = true;
                if (parent.getSelectedClients().Count != 0) {
                    btnTransfer.IsEnabled = true;
                    btnRun.IsEnabled = true;
                }
                
                lblPath.Content = this.filePath;
                //MessageBox.Show(this.filePath + " | " + this.fileName);
            }
        }

        public void ButtonClickable(bool enabled)
        {
            if ((isEnabled) && (enabled))
            {
                btnTransfer.IsEnabled = true;
                btnRun.IsEnabled = true;
            }
            btnDelete.IsEnabled = enabled;

        }

        public void SetProject(string projectName)
        {
            //not relevant
        }

        private void btnTransfer_Click(object sender, RoutedEventArgs e)
        {
            Service.getInstance().CopyFilesToNetworkShare(this.filePath);
            Service.getInstance().CopyFilesFromNetworkShareToClients(this.filePath, this.fileName, parent.getSelectedClients());

            btnDelete.IsEnabled = true;
            
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure you want to delete the entire transferred directory? Verify that result files are backed up and that nothing of value remains in the directory! Do you wish to continue?", "Are you sure?", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                service.deleteFiles(parent.getSelectedClients());
            }
        }

        private void btnRun_Click(object sender, RoutedEventArgs e)
        {
            Service.getInstance().CopyFilesToNetworkShare(this.filePath);
            Service.getInstance().CopyAndRunFilesFromNetworkShareToClients(this.filePath,this.fileName,parent.getSelectedClients());
            btnDelete.IsEnabled = true;
        }

        private void dirBrowse_Btn_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog browse = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = browse.ShowDialog();
            if (result != null){
                this.DirPath = browse.SelectedPath;
                lblDirectoryPath.Content = "Folder to copy: "+this.DirPath;
            }   
        }

        private void dirFileBrowse_Btn_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

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
                    if (i > directory.Length-1)
                    {
                        subFileName += "\\"+file[i];
                    }
                    i++;
                }
                lblFileToLaunch.Content ="File to launch: " + subFileName;
                this.DirFileNameWithExtraDir = subFileName;
            }
               
        }

        private void launchDirectory_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(this.DirFileName);
            Service.getInstance().CopyEntireFolder(parent.getSelectedClients(), this.DirPath, this.DirFileNameWithExtraDir);
        }
    }
}
