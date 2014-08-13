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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Reflection;
using System.Threading;
using System.Net;
using System.Security.Principal;

namespace LabRun
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string testfilepath = "";
        private string testfilename = "";

        public MainWindow()
        {
            InitializeComponent();
            initClients();
            //testRun();
        }

        public void initClients()
        {
            string path = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"clients.txt");
            System.IO.StreamReader file =
   new System.IO.StreamReader(path);

            List<string> clients = new List<string>();
            string line;
            while ((line = file.ReadLine()) != null)
            {
                listBox1.Items.Add(line);
            }
        }

        public void printLDAPcomputers()
        {
            List<string> clients = GetComputers();
            clients.Sort();
            listBox1.ItemsSource = clients;
        }


        public static List<string> GetComputers()
        {
            List<string> ComputerNames = new List<string>();

            DirectoryEntry entry = new DirectoryEntry("LDAP://asb.local", "donatas", "August2014");
            DirectorySearcher mySearcher = new DirectorySearcher(entry);
            mySearcher.Filter = ("(objectClass=computer)");
            mySearcher.SizeLimit = int.MaxValue;
            mySearcher.PageSize = int.MaxValue;

            foreach (SearchResult resEnt in mySearcher.FindAll())
            {
                //"CN=SGSVG007DC"
                string ComputerName = resEnt.GetDirectoryEntry().Name;
                if (ComputerName.StartsWith("CN="))
                    ComputerName = ComputerName.Remove(0, "CN=".Length);
                ComputerNames.Add(ComputerName);
            }

            mySearcher.Dispose();
            entry.Dispose();

            return ComputerNames;
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
                    testfilename = filename.Substring(index+1, filename.Length - (index+1)); // or index + 1 to keep slash
                }

                label1.Content = filename;
                button2.IsEnabled = true;
            }
        }     

        private void xcopy(List<string> computerNames, string srcDir, string dstDir)
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\Users\donatas\Desktop\testCopy.bat"))
            {
                file.WriteLine("@echo off");
                foreach (string computerName in computerNames)
                {
                    string line = @"xcopy """ + srcDir + @""" ""\\" + computerName+@"\test\" + dstDir + @""" /V /E /Y /Q /I";
                    file.WriteLine(line);
                }
            }
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            List<string> computerNames = new  List<string>();
            foreach (string computerName in listBox1.SelectedItems)
            {
                computerNames.Add(computerName);
            }
            string newDir = Path.GetDirectoryName(testfilepath);
            newDir = newDir.Remove(0, newDir.LastIndexOf('\\') + 1);

            xcopy(computerNames, testfilepath, newDir);          

            MessageBox.Show(@"C:\test\" + newDir + @"\" + testfilename);
            runTests(computerNames, @"C:\test\" + newDir + @"\" + testfilename);
        }

        private void runTests(List<string> computerNames, string testExePath){
         
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\Users\donatas\Desktop\testRun.bat"))
            {
                foreach (string computerName in computerNames)
                {
                    string line = @"C:\PSTools\PsExec.exe -d -i \\" + computerName + @" -u asb\labclient -p kPu$27mLi python " + testExePath;
                    file.WriteLine(line);
                 }
            }

            MessageBox.Show("will try to run");
            string strCmdText;
            strCmdText = @"C:\Users\donatas\Desktop\testRun.bat";
            ExecuteCommand(strCmdText);
        }

        private void ExecuteCommand(string command)
        {
            //int exitCode;
            ProcessStartInfo processInfo;
            Process process;

            processInfo = new ProcessStartInfo("cmd.exe", "/c " + command);
            processInfo.CreateNoWindow = true;
            processInfo.UseShellExecute = false;
            // *** Redirect the output ***
            // processInfo.RedirectStandardError = true;
            // processInfo.RedirectStandardOutput = true;

            process = Process.Start(processInfo);
            //process.WaitForExit();

            // *** Read the streams ***
            //string output = process.StandardOutput.ReadToEnd();
            //string error = process.StandardError.ReadToEnd();

            //exitCode = process.ExitCode;

            //MessageBox.Show("output>>" + (String.IsNullOrEmpty(output) ? "(none)" : output));
            //MessageBox.Show("error>>" + (String.IsNullOrEmpty(error) ? "(none)" : error));
            //MessageBox.Show("ExitCode: " + exitCode.ToString(), "ExecuteCommand");
            process.Close();
        }
    }
}
