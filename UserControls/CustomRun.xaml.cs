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
        public HashSet<string> files { get; set; }
        public Boolean isEnabled { get; set; }
        public CustomRun(MainUI parent)
        {
            InitializeComponent();
            files = new HashSet<string>();
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

        }

        public void SetProject(string projectName)
        {
            //not relevant
        }

        private void btnTransfer_Click(object sender, RoutedEventArgs e)
        {
            Service.getInstance().CopyFilesToNetworkShare(this.filePath);
            Service.getInstance().CopyFilesFromNetworkShareToClients(this.filePath, this.fileName, parent.getSelectedClients());
            if (!this.files.Contains(this.filePath))
            {
                this.files.Add(@"C:\labrun\temp\" + this.fileName);
            }
            btnDelete.IsEnabled = true;
            
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            foreach (string temp in this.files) {
                //psexec \\computer cmd /c del fileName
            }
        }

        private void btnRun_Click(object sender, RoutedEventArgs e)
        {
            Service.getInstance().CopyFilesToNetworkShare(this.filePath);
            Service.getInstance().CopyAndRunFilesFromNetworkShareToClients(this.filePath,this.fileName,parent.getSelectedClients());
            if (!this.files.Contains(this.filePath))
            {
                this.files.Add(@"C:\labrun\temp\" + this.fileName);
            }
            btnDelete.IsEnabled = true;
        }
    }
}
