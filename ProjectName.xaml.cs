using ServiceLibrary;
using System.Windows;
using System.Windows.Controls;

namespace LabRun
{
    /// <summary>
    /// Interaction logic for ProjectName.xaml
    /// </summary>
    public partial class ProjectName : Window
    {
        private Service service = Service.getInstance();
        private MainWindow parent = null;
        private string project = "";

        public ProjectName(MainWindow parent, string project)
        {
            InitializeComponent();
            this.parent = parent;
            this.Owner = parent;
            this.project = project;
            txbProjectName.Text = project;
            txbProjectName.Focus();
            txbProjectName.SelectAll();
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            string projectName = txbProjectName.Text;
            parent.SetProject(projectName, true);
            this.Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void txbProjectName_TextChanged(object sender, TextChangedEventArgs e)
        {
            string content = ((TextBox)e.Source).Text;
            btnOk.IsEnabled = !content.Equals("");
        }
    }
}
