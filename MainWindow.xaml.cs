﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Threading;
using ServiceLibrary;
using System.Collections;
using UserControls;

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
        public int labNo = 2;
        private Boolean isSelectionByCmbbx = false;
        private readonly string unnamedProject = "UnnamedProject";
        private string project = "";
        public string Project { get { return this.project; } }
        private Login login;

        public string getProject()
        {
            return project;
        }

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
                if (message == "config.ini")
                {
                    string msg = "File config.ini was not found!";
                    MessageBox.Show(msg, "File not found", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                if (message.Contains("run_with_logs.py"))
                {
                    string msg = "File run_with_logs.py was not found! PsychoPy might not function properly.";
                    MessageBox.Show(msg, "File not found", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            if (!service.FileExists())
            {
                string msg = "File run_with_logs.py was not found! PsychoPy might not function properly.";
                MessageBox.Show(msg, "File not found", MessageBoxButton.OK, MessageBoxImage.Error);
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
            SetProject(unnamedProject);
            service.StartPingSvc(clients);
            service.deleteNetworkTempFiles();
        }

        private void EnableColumn()
        {
            if (clients.Exists(i => i.PsychoPy == true))
                dgrClients.Columns[2].Visibility = Visibility.Visible;
            else dgrClients.Columns[2].Visibility = Visibility.Hidden;
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
            //dgrClients.ItemsSource = selectedClients;
            updateClientsGrid();
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
            tC.TabItem = tabPsy;
            tabControls.Add(tC);
            tabPsy.Header = new TextBlock
            {
                Text = tabPsy.Header.ToString(),
            };

            UserControls.TabControl tC2 = new UserControls.TabControl(this, new EPrime());
            tC2.setTestLogo(@"\Images\eprime.png");
            tabEPrime.Content = tC2;
            tC2.TabItem = tabEPrime;
            tabControls.Add(tC2);
            tabEPrime.Header = new TextBlock
            {
                Text = tabEPrime.Header.ToString(),
            };

            UserControls.TabControl tC3 = new UserControls.TabControl(this, new ZTree());
            tC3.setTestLogo(@"\Images\ztree.png");
            tabZTree.Content = tC3;
            tC3.TabItem = tabZTree;
            tabControls.Add(tC3);
            tabZTree.Header = new TextBlock
                {
                    Text = tabZTree.Header.ToString(),
                };

            ((Button)tC3.FindName("btnRun")).Content = "Run Leaves";
            ((Button)tC3.FindName("btnBrowse")).Visibility = Visibility.Hidden;
            ((TextBlock)tC3.FindName("txbBrowse")).Visibility = Visibility.Hidden;
            ((CheckBox)tC3.FindName("cbxCopyAll")).Visibility = Visibility.Hidden;

            UserControls.ChromeTab tC4 = new UserControls.ChromeTab(this);
            tC4.setTestLogo(@"\Images\chrome-logo.png");
            tabChrome.Content = tC4;
            tC4.TabItem = tabChrome;
            tabControls.Add(tC4);
            tabChrome.Header = new TextBlock
            {
                Text = tabChrome.Header.ToString(),
            };

            UserControls.CustomRun tC5 = new UserControls.CustomRun(this, this.clients);
            tabCustom.Content = tC5;
            tC5.TabItem = tabCustom;
            tabControls.Add(tC5);
            tabCustom.Header = new TextBlock
            {
                Text = tabCustom.Header.ToString(),
            };
        }

        private void SetColumnVisibility(DataGridColumn column, bool visible)
        {
            column.Visibility = visible ? Visibility.Visible : Visibility.Hidden;
            dgrClients.Items.Refresh();
        }

        public void SetFeatureActivity(Feature feature, List<LabClient> selectedClients, bool active)
        {
            bool exists = false;
            DataGridColumn column = null;
            switch (feature)
            {
                case Feature.WEB:
                    selectedClients.ForEach(i => i.Web = active);
                    exists = clients.Exists(i => i.Web == true);
                    column = dgrClients.Columns[6];
                    break;
                case Feature.SHARESCR:
                    selectedClients.ForEach(i => i.ShareScr = active);
                    exists = clients.Exists(i => i.ShareScr == true);
                    column = dgrClients.Columns[7];
                    break;
                case Feature.INPUT:
                    selectedClients.ForEach(i => i.Input = active);
                    exists = clients.Exists(i => i.Input == true);
                    column = dgrClients.Columns[8];
                    break;
            }
            SetColumnVisibility(column, exists);
        }

        public void SetTabActivity(TabItem tabItem, List<LabClient> selectedClients, bool active)
        {
            if (!(tabItem.Header is TextBlock))
            {
                return;
            }

            bool exists = false;
            DataGridColumn column = null;
            switch (tabItem.Name.ToString())
            {
                case "tabPsy":
                    selectedClients.ForEach(i => i.PsychoPy = active);
                    exists = clients.Exists(i => i.PsychoPy == true);
                    column = dgrClients.Columns[2];
                    break;
                case "tabEPrime":
                    selectedClients.ForEach(i => i.EPrime = active);
                    exists = clients.Exists(i => i.EPrime == true);
                    column = dgrClients.Columns[3];
                    break;
                case "tabZTree":
                    selectedClients.ForEach(i => i.ZTree = active);
                    exists = clients.Exists(i => i.ZTree == true);
                    column = dgrClients.Columns[4];
                    break;
                case "tabCustom":
                    selectedClients.ForEach(i => i.Custom = active);
                    exists = clients.Exists(i => i.Custom == true);
                    column = dgrClients.Columns[9];
                    break;
                case "tabChrome":
                    selectedClients.ForEach(i => i.Chrome = active);
                    exists = clients.Exists(i => i.Chrome == true);
                    column = dgrClients.Columns[5];
                    break;
            }
            ((TextBlock)tabItem.Header).Foreground = exists ? Brushes.Red : Brushes.Black;
            SetColumnVisibility(column, exists);
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
            MessageBoxResult result = MessageBox.Show("Are you sure you want to shutdown selected computers?", "Are you sure?", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                lblStatus.Content = "In Progress...";
                service.ShutdownComputers(getSelectedClients());
            }
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
            new About(this).Show();
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
            List<LabClient> clients = getSelectedClients();
            SetFeatureActivity(Feature.INPUT, clients, true);
            service.InputDisable(clients);
        }

        private void btnInputEnable_Click(object sender, RoutedEventArgs e)
        {
            List<LabClient> clients = getSelectedClients();
            SetFeatureActivity(Feature.INPUT, clients, false);
            service.InputEnable(clients);
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            List<LabClient> clients = getSelectedClients();
            SetFeatureActivity(Feature.SHARESCR, clients, true);
            service.StartScreenSharing(clients);
        }

        private void btnNetDisable_Click(object sender, RoutedEventArgs e)
        {
            List<LabClient> clients = getSelectedClients();
            SetFeatureActivity(Feature.WEB, clients, true);
            service.NetDisable(clients);
        }

        private void btnNetEnable_Click(object sender, RoutedEventArgs e)
        {
            List<LabClient> clients = getSelectedClients();
            SetFeatureActivity(Feature.WEB, clients, false);
            service.NetEnable(clients);
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
            List<LabClient> clients = getSelectedClients();
            SetFeatureActivity(Feature.SHARESCR, clients, false);
            service.StopScreenSharing(clients);
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

        private Project projectWnd;
        private ProjectName projectNameWnd;

        private void btnSelProject_Click(object sender, RoutedEventArgs e)
        {
            //if(project Wnd is already open, just show in foreground)
            if (this.projectWnd != null)
            {
                this.projectWnd.Focus();
                return;
            }

            //if(projectName Wnd is already open, just show in foreground)
            if (this.projectNameWnd != null)
            {
                this.projectNameWnd.Focus();
                return;
            }

            if (service.LoggedIn())
            {
                this.projectWnd = new Project(this);
                this.projectWnd.Closed += (senders, args) => this.projectWnd = null;
                this.projectWnd.Show();
            }
            else
            {
                this.projectNameWnd = new ProjectName(this, project);
                this.projectNameWnd.Closed += (senders, args) => this.projectNameWnd = null;
                this.projectNameWnd.Show();
            }
        }

        public void SetProject(string projectName, bool checkForExistingProject = false)
        {
            if (checkForExistingProject && service.LocalProjectExists(project))
            {
                MessageBoxResult result = MessageBox.Show("Previous project had some data! Would you like to move that data from the old project to this new project?", "Warning", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Cancel)
                {
                    return;
                }

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        service.MoveProject(project, projectName);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Unable to rename the folder. Make sure that there are no open files or explorer windows from that directory.\n\n" + ex.Message, "Failed to rename", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }

            project = projectName;
            lblProject.Text = project;
            foreach (ControlUnit cUnit in tabControls)
            {
                cUnit.SetProject(project);
            }
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            if (!service.LoggedIn())
            {
                //if(login is already open, just show in foreground)
                if (this.login != null)
                {
                    this.login.Focus();
                    return;
                }
                this.login = new Login(this);
                this.login.Closed += (senders, args) => this.login = null;
                this.login.Show();
            }
            else
            {
                service.LogOut();
                btnLogin.Content = "Login";
                lblLogin.Content = "Logged in as Guest";
                btnSelProject.Content = "Set Your Project";
                SetProject(unnamedProject);
            }
        }

        public void SetLogin(User user)
        {
            lblLogin.Content = "Logged in as " + user.Username;
            btnSelProject.Content = "Choose Your Project";
            btnLogin.Content = "Logout";
        }

        private void AURPS_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://aucobe.sona-systems.com/");
        }

        private void AUCBL_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://cobelab.au.dk/");
        }

        private void COBELAB_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("http://bss.au.dk/research/research-labs/cognition-and-behavior-lab/");
        }

        private void btnFwUpdate(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("This will update firewall rules for lab computers to allow speedy remote launches. Make sure that all lab computers are turned on.\n\nAre you sure you want to continue?", "Are you sure?", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                service.UpdateFirewallRules(clients);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Unable to get IP Address", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BRIDGE_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("http://cobelab.au.dk:8000");
        }

        private void btnConfEdit(object sender, RoutedEventArgs e)
        {
            Process.Start("config.ini");
        }
    }
}



