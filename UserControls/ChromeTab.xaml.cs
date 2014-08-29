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
            string param1 = "";
            string newWindowMode = ((ComboBoxItem)cmbBoxNewWindowMode.SelectedItem).Tag.ToString();
            switch (newWindowMode)
            {
                case "newtab":
                    {
                        break;
                    }
                case "newwindow":
                    {
                        param1 = " --new-window";
                        break;
                    }
                case "newchrome":
                    {
                        foreach (LabClient client in parent.getSelectedClients())
                        {
                            Service.getInstance().killRemoteProcess(client.ComputerName, "Chrome.exe");
                        }
                        break;
                    }

            }

            string param = param1 + " "; 
            string windowMode = ((ComboBoxItem)cmbBoxWindowMode.SelectedItem).Tag.ToString();
            switch (windowMode)
            {
                case "1":
                    {
                        param += " -start-maximized";
                        break;
                    }
                case "2":
                    {
                        param += " -fullscreen";
                        break;
                    }
                case "3":
                    {
                        param += " -kiosk";
                        break;
                    }

            }
            param += " " + urlTxtBox.Text.ToString();
            Service.getInstance().runRemoteProgram(parent.getSelectedClients(), @"C:\Program Files (x86)\Google\Chrome\Application\Chrome.exe", param);
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            foreach (LabClient client in parent.getSelectedClients())
            {
                Service.getInstance().killRemoteProcess(client.ComputerName, "Chrome.exe");
            }
            
        }

        public void ButtonClickable(bool enabled)
        {
            btnRun.IsEnabled = enabled;
            btnClose.IsEnabled = enabled;
        }
    }
}
