﻿using System;
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

namespace LabRun
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, MainUI
    {
        private Service service;
        List<LabClient> clients = new List<LabClient>();

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

        }
        public void initClients()
        {
            List<LabClient> mylist = new List<LabClient>();
            mylist = service.GetLabComputersFromStorage();
            dgrClients.ItemsSource = mylist;
        }




        private void button1_Click(object sender, RoutedEventArgs e)
        {


        }

        private void initTabs()
        {
            UserControls.TabControl tC = new UserControls.TabControl(this, new PsychoPy());
            tC.setTestLogo(@"\Images\Psychopy.png");
            tabPsy.Content = tC;

            UserControls.TabControl tC2 = new UserControls.TabControl(this, new EPrime());
            tC2.setTestLogo(@"\Images\eprime.png");
            tabEPrime.Content = tC2;

            UserControls.TabControl tC3 = new UserControls.TabControl(this, new ZTree());
            tC3.setTestLogo(@"\Images\ztree.png");
            tabZTree.Content = tC3;
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
            service.ShutdownComputer(getSelectedClientsNames());
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

        private void btnEven_Click(object sender, RoutedEventArgs e)
        {
            List<LabClient> clients = (List<LabClient>)dgrClients.ItemsSource;

            IEnumerable<LabClient> emp = (from i in clients
                                          where i.BoothNo % 2 == 0
                                          select i);

            dgrClients.SelectedItems.Clear();
            foreach (LabClient es in emp)
            {
                dgrClients.SelectedItems.Add(es);
            }
        }

        private void btnOdd_Click(object sender, RoutedEventArgs e)
        {
            List<LabClient> clients = (List<LabClient>)dgrClients.ItemsSource;

            IEnumerable<LabClient> emp = (from i in clients
                                          where ((i.BoothNo % 2 != 0) && i.BoothNo != null)
                                          select i);

            dgrClients.SelectedItems.Clear();
            foreach (LabClient es in emp)
            {
                dgrClients.SelectedItems.Add(es);
            }
        }

        private void btnzigzag_Click(object sender, RoutedEventArgs e)
        {
            List<LabClient> clients = (List<LabClient>)dgrClients.ItemsSource;
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

            dgrClients.SelectedItems.Clear();
            foreach (LabClient es in clientsSelected)
            {
                dgrClients.SelectedItems.Add(es);
            }
        }

        private void btnzagzig_Click(object sender, RoutedEventArgs e)
        {
            List<LabClient> clients = (List<LabClient>)dgrClients.ItemsSource;
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

            dgrClients.SelectedItems.Clear();
            foreach (LabClient es in clientsSelected)
            {
                dgrClients.SelectedItems.Add(es);
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
                    dgrClients.ItemsSource = service.GetLabComputersNew2();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("ARP error! The computer is not listed in the ARP pool. Restart client computers to solve the problem.", "ARP error!");
                }
            }

        }




        public void updateStatus(string msg)
        {
            lblStatus.Content = msg;
        }
    }
}

