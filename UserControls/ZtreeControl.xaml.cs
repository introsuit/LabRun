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
using System.IO;
using System.Reflection;


namespace UserControls
{
    /// <summary>
    /// Interaction logic for ZtreeControl.xaml
    /// </summary>
    public partial class ZtreeControl : UserControl
    {
        private Service service = Service.getInstance();
        private ZTree ztree = null;

        public ZtreeControl(ZTree ztree)
        {
            InitializeComponent();
            this.ztree = ztree;
            cmbWindowSizes.ItemsSource = service.WindowSizes;
        }

        public WindowSize GetSelectedWindowSize()
        {
            return (WindowSize)cmbWindowSizes.SelectedItem;
        }

        private void btnRunAdminZTree_Click(object sender, RoutedEventArgs e)
        {
            ztree.RunAdminZTree();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                path = System.IO.Path.Combine(path, "merge.exe");
                System.Diagnostics.Process.Start(path);
            }
            catch (Exception ex)
            {
                MessageBox.Show("The program for merging the pay file into a Word doument was not found! \n\n" + ex.ToString(), "File not found!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
