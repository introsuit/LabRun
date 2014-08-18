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
        private string testDirName = "";

        private string tempPath = System.IO.Path.GetTempPath();
        private string labClientSharedFolder = @"C:\test\";
        private string labClientSharedFolderName = "test";

        public MainWindow()
        {
            InitializeComponent();
            initClients();
        }
        public void initClients()
        {
            foreach (string client in service.GetLabComputersFromFile())
            {
                listBox1.Items.Add(client);
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
                    testfilepath = filename.Substring(0, index+1); // or index + 1 to keep slash
                    testfilename = filename.Substring(index + 1, filename.Length - (index + 1)); // or index + 1 to keep slash
                }


                string newDir = Path.GetDirectoryName(testfilepath);
                testDirName = newDir.Remove(0, newDir.LastIndexOf('\\') + 1);

                label1.Content = filename;
                button2.IsEnabled = true;
            }
        }

        private void robocopy(string srcDir, string dstDir)
        {
            string copyPath = tempPath + "testCopy.bat";
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(copyPath))
            {
                file.WriteLine("@echo off");
                foreach (string computerName in getSelectedClients())
                {
                    file.WriteLine(@"NET USE \\" + computerName + @"\test /USER:labclient@asb kPu$27mLi");
                    file.WriteLine(@"robocopy """ + srcDir + @""" ""\\" + computerName + @"\test\" + dstDir + @""" /S");
                    file.WriteLine(@"NET USE \\" + computerName + @"\test /D");
                }
            }
            //MessageBox.Show(copyPath);
            service.ExecuteCommandNoOutput(copyPath, true);
        }

        private void xcopy(string srcDir, string dstDir)
        {
            string copyPath = tempPath + "testCopy.bat";
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(copyPath))
            {
                file.WriteLine("@echo off");
                foreach (string computerName in getSelectedClients())
                {
                    file.WriteLine(@"net use ""\\" + computerName + @"\test"" kPu$27mLi /user:asb\labclient");

                    file.WriteLine(":copy");
                    string line = @"xcopy """ + srcDir + @""" ""\\" + computerName + @"\test\" + dstDir + @""" /V /E /Y /Q /I";
                    file.WriteLine(line);
                    file.WriteLine("IF ERRORLEVEL 0 goto disconnect");
                    file.WriteLine("goto end");

                    file.WriteLine(":disconnect");
                    file.WriteLine(@"net use ""\\" + computerName + @"\test"" /delete");
                    file.WriteLine("goto end");
                    file.WriteLine(":end");
                }
            }
            //MessageBox.Show(copyPath);
            service.ExecuteCommandNoOutput(copyPath, true);
        }

        private void xcopyResults(string dstDir)
        {
            string copyPath = tempPath + "testCopyResults.bat";
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(copyPath))
            {
                file.WriteLine("@echo off");
                foreach (string computerName in getSelectedClients())
                {
                    file.WriteLine(@"net use ""\\" + computerName + @"\test"" kPu$27mLi /user:asb\labclient");

                    file.WriteLine(":copy");
                    string path = @"\\" + computerName + @"\" + labClientSharedFolderName + @"\" + testDirName;

                    string line = @"xcopy """ + path + @"\*.psydat"" """ + dstDir + @"\" + testDirName + @""" /V /E /Y /Q /I";
                    file.WriteLine(line);
                    line = @"xcopy """ + path + @"\*.csv"" """ + dstDir + @"\" + testDirName + @""" /V /E /Y /Q /I";
                    file.WriteLine(line);
                    line = @"xcopy """ + path + @"\*.log"" """ + dstDir + @"\" + testDirName + @""" /V /E /Y /Q /I";
                    file.WriteLine(line);

                    file.WriteLine("IF ERRORLEVEL 0 goto disconnect");
                    file.WriteLine("goto end");

                    file.WriteLine(":disconnect");
                    file.WriteLine(@"net use ""\\" + computerName + @"\test"" /delete");
                    file.WriteLine("goto end");
                    file.WriteLine(":end");
                }
            }
            //MessageBox.Show(copyPath);
            service.ExecuteCommandNoOutput(copyPath, true);
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

          

            xcopy(testfilepath.Substring(0, testfilepath.Length - 1), testDirName);
            runTests(computerNames, labClientSharedFolder + testDirName + @"\" + testfilename);
            btnKill.IsEnabled = true;
            //MessageBox.Show("Request done");
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
            //MessageBox.Show(strCmdText);
            service.runCmd(strCmdText);
        }

        private void runTests(List<string> computerNames, string testExePath)
        {
            int i = 0;
            foreach (string computerName in computerNames)
            {
                string runPath = tempPath + "testRun" + i + ".bat";
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(runPath))
                {

                    string line = @"C:\PSTools\PsExec.exe -d -i 1 \\" + computerName + @" -u asb\labclient -p kPu$27mLi python " + testExePath;
                   // string line = @"C:\PSTools\PsExec.exe -d -i \\" + computerName + @" -u mano.local\Administrator -p Mandrass1 python " + testExePath;
                    file.WriteLine(line);
                }

                //MessageBox.Show(runPath);
                StartNewCmdThread(runPath);

                i++;
            }
        }

        private List<string> getSelectedClients()
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
            foreach (string computerName in getSelectedClients())
            {
                service.killRemoteProcess(computerName, "python.exe");
            }
        }

        private void btnGetResults_Click(object sender, RoutedEventArgs e)
        {
            
            xcopyResults(@"C:\Dump");
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
            service.ShutdownComputer(null);
        }
    }
}
