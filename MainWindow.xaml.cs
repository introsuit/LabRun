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

namespace LabRun
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Service service = Service.getInstance();

        private string testfilepath = "";
        private string testfilename = "";
        private string testDirName = "";

        
        private string labClientSharedFolder = @"C:\test\";
        private string labClientSharedFolderName = "test";

        public MainWindow()
        {
            InitializeComponent();
            initClients();
        }
        public void initClients()
        {
            dgrClients.ItemsSource = service.GetLabComputersNew();
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension 
            dlg.DefaultExt = ".py";
            dlg.Filter = "Python files (*.py)|*.py|PsychoPy Test Files (*.psyexp)|*.psyexp";

            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                // Open document 
                string filename = dlg.FileName;

                int index = filename.LastIndexOf("\\");
                if (index > 0)
                {
                    testfilepath = filename.Substring(0, index+1); // or index + 1 to keep slash
                    testfilename = filename.Substring(index + 1, filename.Length - (index + 1)); // or index + 1 to keep slash
                }


                string newDir = Path.GetDirectoryName(testfilepath);
                testDirName = newDir.Remove(0, newDir.LastIndexOf('\\') + 1);

                label1.Content = filename;
                button2.IsEnabled = true;
            }
        }     

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            List<string> computerNames = getSelectedClients();

            if (computerNames.Count == 0)
            {
                MessageBox.Show("Select some clients first!");
                return;
            }

            service.xcopyPsychoPy(testfilepath.Substring(0, testfilepath.Length - 1), testDirName, computerNames);
            service.runPsychoPyTests(computerNames, labClientSharedFolder + testDirName + @"\" + testfilename);
            btnKill.IsEnabled = true;
            //MessageBox.Show("Request done");
        }     

        private List<string> getSelectedClients()
        {
            List<LabClient> clients = dgrClients.SelectedItems.Cast<LabClient>().ToList(); ;
            
            List<string> computerNames = new List<string>();
            foreach (LabClient client in clients)
            {
                computerNames.Add(client.ComputerName);
            }
            return computerNames;
        }

        private void btnKill_Click(object sender, RoutedEventArgs e)
        {
            foreach (string computerName in getSelectedClients())
            {
                service.killRemoteProcess(computerName, "python.exe");
            }
        }

        private void btnGetResults_Click(object sender, RoutedEventArgs e)
        {
            string srcWithoutComputerName = @"\" + labClientSharedFolderName + @"\" + testDirName;
            string dstFolderName = testDirName;
            service.xcopyPsychoPyResults(srcWithoutComputerName, dstFolderName, getSelectedClients());
            //foreach (string computerName in getSelectedClients())
            //{
            //    string path = @"\\" + computerName + @"\" + labClientSharedFolderName + @"\" + testDirName;
            //    Debug.WriteLine(path);
            //    foreach (string file in service.GetFiles(path))
            //    {
            //        //check for result types. also make sure it is case insensitive
            //        if (file.EndsWith(".psydat", true, null) || file.EndsWith(".log", true, null) || file.EndsWith(".csv", true, null))
            //        {
            //            Debug.WriteLine(file);
            //            //File.Copy(file, );
            //        }
            //    }
            //}
        }

        private void btnShutdown_Click(object sender, RoutedEventArgs e)
        {
            service.ShutdownComputer(getSelectedClients());
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
           var rows= GetDataGridRows(dgrClients);

           foreach (DataGridRow r in rows)
           {
               DataRowView rv = (DataRowView)r.Item;
               foreach (DataGridColumn column in dgrClients.Columns)
               {
                   if (column.GetCellContent(r) is TextBlock)
                   {
                       TextBlock cellContent = column.GetCellContent(r) as TextBlock;
                       MessageBox.Show(cellContent.Text);
                   }
               }
           }
        }
    }
}
