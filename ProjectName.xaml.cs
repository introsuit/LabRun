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

namespace LabRun
{
    /// <summary>
    /// Interaction logic for ProjectName.xaml
    /// </summary>
    public partial class ProjectName : Window
    {
        private MainWindow parent = null;
        private string project = "";

        public ProjectName(MainWindow parent, string project)
        {
            InitializeComponent();
            this.parent = parent;
            this.project = project;
            txbProjectName.Text = project;
            txbProjectName.Focus();
            txbProjectName.SelectAll();
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            string projectName = txbProjectName.Text;
            parent.SetProject(projectName);
            this.Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
