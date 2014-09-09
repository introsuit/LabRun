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
        public CustomRun(MainUI parent)
        {
            InitializeComponent();
            this.parent = parent;
        }

        private void browse_Btn_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension 
            //dlg.DefaultExt = testApp.Extension;
            //dlg.Filter = testApp.ExtensionDescription;

            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                // Open document 
                string testFullPath = dlg.FileName;
                
            }
        }


        public void ButtonClickable(bool enabled)
        {
            btnTransfer.IsEnabled = enabled;
        }

        public void SetProject(string projectName)
        {
            //not relevant
        }

        private void btnRun_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
