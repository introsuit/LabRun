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

        }
    }
}
