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
using UserControls;

namespace LabRun
{
    /// <summary>
    /// Interaction logic for Project.xaml
    /// </summary>
    public partial class Project : Window
    {
        private Service service = Service.getInstance();
        private List<string> projects = new List<string>();
        private MainUI parent = null;

        public Project(MainUI parent)
        {
            InitializeComponent();
            this.parent = parent;
            projects = service.GetProjects();
            lstProjects.ItemsSource = projects;
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            string project = (string)lstProjects.SelectedValue;
            if (project == null)
                return;
            parent.SetProject(project);
            this.Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void lstProjects_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstProjects.SelectedIndex != 0)
            {
                btnOK.IsEnabled = true;
            }
            else
            {
                btnOK.IsEnabled = false;
            }
        }

        private void lstProjects_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            //int index = lstProjects.inde(e.Location);
            //if (index != System.Windows.Forms.ListBox.NoMatches)
            //{
            //    MessageBox.Show(index.ToString());
            //}
        }
    }
}
