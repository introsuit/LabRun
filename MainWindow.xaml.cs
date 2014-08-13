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

        private string tempPath = System.IO.Path.GetTempPath();
        private string labClientSharedFolder = @"C:\test\";

        public MainWindow()
        {
            InitializeComponent();
            initClients();
        }

        public void initClients()
        {
            string path = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"clients.txt");
            System.IO.StreamReader file = new System.IO.StreamReader(path);

            List<string> clients = new List<string>();
            string line;
            while ((line = file.ReadLine()) != null)
            {
                listBox1.Items.Add(line);
            }
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
                    testfilepath = filename.Substring(0, index); // or index + 1 to keep slash
                    testfilename = filename.Substring(index + 1, filename.Length - (index + 1)); // or index + 1 to keep slash
                }

                label1.Content = filename;
                button2.IsEnabled = true;
            }
        }

        private void xcopy(string srcDir, string dstDir)
        {
            string copyPath = tempPath + "testCopy.bat";
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(copyPath))
            {
                file.WriteLine("@echo off");
                foreach (string computerName in getComputerNamesList())
                {
                    string line = @"xcopy """ + srcDir + @""" ""\\" + computerName + @"\test\" + dstDir + @""" /V /E /Y /Q /I";
                    file.WriteLine(line);
                }
            }

            service.ExecuteCommand(copyPath, true);
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            List<string> computerNames = new List<string>();
            foreach (string computerName in listBox1.SelectedItems)
            {
                computerNames.Add(computerName);
            }

            if (computerNames.Count == 0)
            {
                MessageBox.Show("Select some clients first!");
                return;
            }

            string newDir = Path.GetDirectoryName(testfilepath);
            newDir = newDir.Remove(0, newDir.LastIndexOf('\\') + 1);

            xcopy(testfilepath, newDir);
            runTests(computerNames, labClientSharedFolder + newDir + @"\" + testfilename);
            btnKill.IsEnabled = true;
            MessageBox.Show("Request done");

        }

        public Thread StartNewCmdThread(string cmd)
        {
            var t = new Thread(() => RealStart(cmd));
            t.Start();
            return t;
        }

        private void RealStart(string cmd)
        {
            string strCmdText = @cmd;
            service.ExecuteCommand(strCmdText);
        }

        private void runTests(List<string> computerNames, string testExePath)
        {
            int i = 0;
            foreach (string computerName in computerNames)
            {
                string runPath = tempPath + "testRun" + i + ".bat";
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(runPath))
                {

                    //string line = @"C:\PSTools\PsExec.exe -d -i \\" + computerName + @" -u asb\labclient -p kPu$27mLi python " + testExePath;
                    string line = @"C:\PSTools\PsExec.exe -d -i \\" + computerName + @" -u mano.local\Administrator -p Mandrass1 python " + testExePath;
                    file.WriteLine(line);
                }

                StartNewCmdThread(runPath);

                i++;
            }
        }

        private List<string> getComputerNamesList()
        {
            List<string> computerNames = new List<string>();
            foreach (string computerName in listBox1.SelectedItems)
            {
                computerNames.Add(computerName);
            }
            return computerNames;
        }

        private void btnKill_Click(object sender, RoutedEventArgs e)
        {
            foreach (string computerName in getComputerNamesList())
            {
                service.killRemoteProcess(computerName, "python.exe");
            }
        }
    }
}
