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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace UserControls
{
    /// <summary>
    /// Interaction logic for Page1.xaml
    /// </summary>
    public partial class ChromeTab : UserControl, ControlUnit
    {
        private MainUI parent = null;
        private Service service = Service.getInstance();

        public ChromeTab(MainUI parent)
        {
            InitializeComponent();
            this.parent = parent;
        }

        public void setTestLogo(string path)
        {
            var uriSource = new Uri(path, UriKind.Relative);
            imgTest.Source = new BitmapImage(uriSource);
        }
        
        private void btnRun_Click(object sender, RoutedEventArgs e)
        {
            service.runRemoteProgram(parent.getSelectedClients(), @"C:\Program Files (x86)\Google\Chrome\Application\Chrome.exe", urlTxtBox.Text.ToString());
        }

        public void ButtonClickable(bool enabled)
        {
            btnRun.IsEnabled = enabled;
            btnKill.IsEnabled = enabled;
        }
    }
}
