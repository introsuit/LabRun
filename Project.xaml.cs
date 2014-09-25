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
        private MainWindow parent = null;

        public Project(MainWindow parent)
        {
            InitializeComponent();
            this.parent = parent;
            this.Owner = parent;
            projects = service.GetProjects();
            lstProjects.ItemsSource = projects;
        }

        private void UpdateSelectedProject(string project)
        {
            if (project == null)
                return;
            parent.SetProject(project, true);
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            string project = (string)lstProjects.SelectedValue;
            UpdateSelectedProject(project);
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

        protected void HandleDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var project = ((ListBoxItem)sender).Content as string; //Casting back to the binded Track
            UpdateSelectedProject(project);
            this.Close();
        }
    }
}
