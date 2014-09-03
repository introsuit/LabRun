using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Reflection;
using System.Threading;
using System.Net;
using ServiceLibrary;
using System.Collections;
using System.Data;
using UserControls;
using System.Net.NetworkInformation;

namespace LabRun
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, MainUI
    {
        private Service service;
        private List<LabClient> clients = new List<LabClient>();
        private List<ControlUnit> tabControls = new List<ControlUnit>();
        public int labNo = 1;
        private Boolean isSelectionByCmbbx = false;

        public MainWindow()
        {
            InitializeComponent();
            try
            {
                service = Service.getInstance();
            }
            catch (FileNotFoundException ex)
            {
                string message = ex.Message;
                if (message == "auth.ini")
                {
                    string msg = "\n\nauth.ini file must be created and set with data in order:\n\nDomain\nUserName\nPassword";
                    MessageBox.Show(ex.InnerException.Message + msg, "File not found", MessageBoxButton.OK, MessageBoxImage.Error);
                    this.Close();
                    return;
                }
            }

            service.ProgressUpdate += (s, e) =>
            {
                Dispatcher.Invoke((Action)delegate()
                {
                    StatusEventArgs args = (StatusEventArgs)e;
                    lblStatus.Content = args.Message;
                }
            );
            };
            initClients();
            initTabs();
            service.StartPingSvc(clients);
        }
        public void initClients()
        {
            try
            {
                clients = service.GetLabComputersFromStorage();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            List<LabClient> selectedClients = service.filterForRoom(clients, labNo);
            dgrClients.ItemsSource = selectedClients;
        }

        public void updateClientsGrid()
        {
            if (labNo == 0)
            {
                dgrClients.ItemsSource = this.clients;
            }
            else
            {
                List<LabClient> selectedClients = service.filterForRoom(clients, labNo);
                dgrClients.ItemsSource = selectedClients;
            }
        }

        private void initTabs()
        {
            UserControls.TabControl tC = new UserControls.TabControl(this, new PsychoPy());
            tC.setTestLogo(@"\Images\Psychopy.png");
            tabPsy.Content = tC;
            tabControls.Add(tC);

            UserControls.TabControl tC2 = new UserControls.TabControl(this, new EPrime());
            tC2.setTestLogo(@"\Images\eprime.png");
            tabEPrime.Content = tC2;
            tabControls.Add(tC2);

            UserControls.TabControl tC3 = new UserControls.TabControl(this, new ZTree());
            tC3.setTestLogo(@"\Images\ztree.png");
            tabZTree.Content = tC3;
            tabControls.Add(tC3);

            ((Label)tC3.FindName("lblWindowSize")).Visibility = Visibility.Visible;
            ComboBox cmbWinSizes = (ComboBox)tC3.FindName("cmbWindowSizes");
            cmbWinSizes.Visibility = Visibility.Visible;
            cmbWinSizes.ItemsSource = service.WindowSizes;
            ((Button)tC3.FindName("btnBrowse")).Visibility = Visibility.Hidden;
            ((Label)tC3.FindName("lblBrowse")).Visibility = Visibility.Hidden;
            ((Label)tC3.FindName("lblZTreeInfo")).Visibility = Visibility.Visible;

            UserControls.ChromeTab tC4 = new UserControls.ChromeTab(this);
            tC4.setTestLogo(@"\Images\chrome-logo.png");
            tabChrome.Content = tC4;
            tabControls.Add(tC4);
        }

        public List<LabClient> getSelectedClients()
        {
            return dgrClients.SelectedItems.Cast<LabClient>().ToList();
        }

        public List<string> getSelectedClientsNames()
        {
            List<LabClient> clients = dgrClients.SelectedItems.Cast<LabClient>().ToList();

            List<string> computerNames = new List<string>();
            foreach (LabClient client in clients)
            {
                computerNames.Add(client.ComputerName);
            }
            return computerNames;
        }

        private List<string> getSelectedCompsMacs()
        {
            List<string> selectedMACs = new List<string>();
            List<LabClient> clients = dgrClients.SelectedItems.Cast<LabClient>().ToList();
            foreach (LabClient client in clients)
            {
                selectedMACs.Add(client.Mac);
            }
            return selectedMACs;
        }

        private void btnShutdown_Click(object sender, RoutedEventArgs e)
        {
            lblStatus.Content = "In Progress...";
            service.ShutdownComputers(getSelectedClients());
        }

        public IEnumerable<DataGridRow> GetDataGridRows(DataGrid grid)
        {
            var itemsSource = grid.ItemsSource as IEnumerable;
            if (null == itemsSource) yield return null;
            foreach (var item in itemsSource)
            {
                var row = grid.ItemContainerGenerator.ContainerFromItem(item) as DataGridRow;
                if (null != row) yield return row;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            List<string> list = getSelectedCompsMacs();
            foreach (string mac in list)
            {
                try
                {
                    MACAddress.SendWOLPacket(mac);

                }
                catch (Exception Error)
                {
                    MessageBox.Show(
                    string.Format("Error:\n\n{0}", Error.Message), "Error");
                }
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure you want to update the list of computers? Please make sure that the computers are all turned on after the server, to enable discovery. Check ARP list to be sure or restart all lab computers manually. Do you wish to continue?", "Are you sure?", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    dgrClients.ItemsSource = service.GetLabComputersNew2(labNo);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("ARP error! The computer is not listed in the ARP pool. Restart client computers to solve the problem. \n" + ex.Message, "ARP error!");
                }
            }
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            new About().Show();
        }

        public void updateStatus(string msg)
        {
            lblStatus.Content = msg;
        }

        private void cmbBxLabSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (service == null)
                return;

            string labNo = ((ComboBoxItem)cmbBxLabSelect.SelectedItem).Tag.ToString();
            switch (labNo)
            {
                case "lab1":
                    {
                        this.labNo = 1;
                        break;
                    }
                case "lab2":
                    {
                        this.labNo = 2;
                        break;
                    }
                case "both":
                    {
                        this.labNo = 0;
                        break;
                    }
            }
            updateClientsGrid();
            cmbSelectionClients.SelectedIndex = 0;
        }

        private void dgrClients_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool smthSelected = dgrClients.SelectedItems.Count > 0;
            foreach (ControlUnit tab in tabControls)
            {
                tab.ButtonClickable(smthSelected);
            }
            btnStartUp.IsEnabled = smthSelected;
            btnShutdown.IsEnabled = smthSelected;
            btnInputDisable.IsEnabled = smthSelected;
            btnInputEnable.IsEnabled = smthSelected;
            btnNetDisable.IsEnabled = smthSelected;
            btnNetEnable.IsEnabled = smthSelected;
            BtnScrShare.IsEnabled = smthSelected;
            btnStopSharing.IsEnabled = smthSelected;
            if (!this.isSelectionByCmbbx)
                cmbSelectionClients.SelectedIndex = 1;
            
        }

        private void btnInputDisable_Click(object sender, RoutedEventArgs e)
        {
            service.InputDisable(getSelectedClients());
        }

        private void btnInputEnable_Click(object sender, RoutedEventArgs e)
        {
            service.InputEnable(getSelectedClients());
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            service.StartScreenSharing(getSelectedClients());
        }

        private void btnNetDisable_Click(object sender, RoutedEventArgs e)
        {
            service.NetDisable(getSelectedClients());
        }

        private void btnNetEnable_Click(object sender, RoutedEventArgs e)
        {
            service.NetEnable(getSelectedClients());
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            service.StopAndClean();
            lock (service.key)
            {
                Monitor.Pulse(service.key);
            }
        }

        private void SelectClients(List<LabClient> clients)
        {
            dgrClients.SelectedItems.Clear();
            foreach (LabClient client in clients)
            {
                dgrClients.SelectedItems.Add(client);
            }
        }

        private void MenuItemExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void cmbSelectionClients_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            List<LabClient> clients = (List<LabClient>)dgrClients.ItemsSource;

            string selection = ((ComboBoxItem)cmbSelectionClients.SelectedItem).Tag.ToString();
            switch (selection)
            {
                case "all":
                    {
                        this.isSelectionByCmbbx = true;
                        SelectClients(clients);
                        break;
                    }
                case "none":
                    {
                        this.isSelectionByCmbbx = true;
                        dgrClients.SelectedItems.Clear();
                        break;
                    }
                case "odd":
                    {
                        this.isSelectionByCmbbx = true;
                        clients = clients.Where(i => i.BoothNo % 2 != 0).ToList();
                        SelectClients(clients);
                        break;
                    }
                case "even":
                    {
                        this.isSelectionByCmbbx = true;
                        clients = clients.Where(i => i.BoothNo % 2 == 0).ToList();
                        SelectClients(clients);
                        break;
                    }
                case "zigzag":
                    {
                        this.isSelectionByCmbbx = true;
                        List<LabClient> clientsSelected = new List<LabClient>();
                        Boolean even = true;
                        Boolean odd = false;

                        foreach (LabClient client in clients)
                        {

                            //Selecting every second odd
                            if (client.BoothNo % 2 == 0)
                            {
                                if (odd)
                                {
                                    odd = false;
                                    clientsSelected.Add(client);
                                }
                                else
                                    odd = true;
                            }

                            //Selecting every first even
                            if ((client.BoothNo % 2 != 0) && client.BoothNo != null)
                            {
                                if (even)
                                {
                                    even = false;
                                    clientsSelected.Add(client);
                                }
                                else
                                    even = true;
                            }
                        }
                        SelectClients(clientsSelected);
                        break;
                    }
                case "zagzig":
                    {
                        this.isSelectionByCmbbx = true;
                        List<LabClient> clientsSelected = new List<LabClient>();
                        Boolean even = false;
                        Boolean odd = true;

                        foreach (LabClient client in clients)
                        {

                            //Selecting every first odd
                            if (client.BoothNo % 2 == 0)
                            {
                                if (odd)
                                {
                                    odd = false;
                                    clientsSelected.Add(client);
                                }
                                else
                                    odd = true;
                            }

                            //Selecting every second even
                            if ((client.BoothNo % 2 != 0) && client.BoothNo != null)
                            {
                                if (even)
                                {
                                    even = false;
                                    clientsSelected.Add(client);
                                }
                                else
                                    even = true;
                            }
                        }
                        SelectClients(clientsSelected);
                        break;
                    }
            }
        }

        private void btnStopSharing_Click(object sender, RoutedEventArgs e)
        {
            service.StopScreenSharing(getSelectedClients());

        }

        private void dgrClients_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.isSelectionByCmbbx = false;
            cmbSelectionClients.SelectedIndex = 1;
        }

        private void dgrClients_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.isSelectionByCmbbx = false;
            cmbSelectionClients.SelectedIndex = 1;
        }

      

        private void dgrClients_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.isSelectionByCmbbx = false;
            cmbSelectionClients.SelectedIndex = 1;
        }

        private void dgrClients_MouseLeftButtonUp_1(object sender, MouseButtonEventArgs e)
        {
            this.isSelectionByCmbbx = false;
            cmbSelectionClients.SelectedIndex = 1;
        }

        private void dgrClients_MouseUp_1(object sender, MouseButtonEventArgs e)
        {
            this.isSelectionByCmbbx = false;
            cmbSelectionClients.SelectedIndex = 1;
        }
    }
}



