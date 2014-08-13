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

        private void testRun()
        {
            string strCmdText;
            strCmdText = "\"C:\\PsTools\\PsExec.exe\" -i -d  \\YLGW036496 -u asb\\labclient -p kPu$27mLi python \"C:\\Cobe Lab\\Psychopy\\Price\\Price1AI.py\"";
            
            System.Diagnostics.Process.Start("CMD.exe", strCmdText);

           // System.Diagnostics.Process.Start("c:\\run.bat");

            //System.Diagnostics.Process process = new System.Diagnostics.Process();
            //System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            //startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            //startInfo.FileName = "cmd.exe";
            //startInfo.Arguments = "\"C:\\PsTools\\PsExec.exe\" -i -d  \\YLGW036496 -u asb\\labclient -p kPu$27mLi python \"C:\\Cobe Lab\\Psychopy\\Price\\Price1AI.py\"";
            //process.StartInfo = startInfo;
            //System.Diagnostics.Process.Start("\"C:\\PsTools\\PsExec.exe\" -i -d  \\YLGW036496 -u asb\\labclient -p kPu$27mLi python \"C:\\Cobe Lab\\Psychopy\\Price\\Price1AI.py\"");
        }

        private string copyTestDir(string srcDir, string dstDir)
        {
            // create new dst dir
            string newDir = Path.GetDirectoryName(srcDir);
            newDir = newDir.Remove(0, newDir.LastIndexOf('\\') + 1);         
            Directory.CreateDirectory(dstDir + "\\" + newDir);

            

            // update dst with new dir
            dstDir = dstDir + "\\" + newDir + "\\";

            // substring is to remove destination_dir absolute path (E:\).
            // Create subdirectory structure in destination    
            foreach (string dir in Directory.GetDirectories(srcDir, "*", System.IO.SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dstDir + dir.Substring(srcDir.Length));
                // Example:
                //     > C:\sources (and not C:\E:\sources)
            }


            
            foreach (string file_name in Directory.GetFiles(srcDir, "*.*", System.IO.SearchOption.AllDirectories))
            {

                File.Copy(file_name, file_name+"_backup", true);
               File.Move(file_name, dstDir + file_name.Substring(srcDir.Length));
               File.Move(file_name + "_backup", file_name);
               
                    //File.Copy(file_name, dstDir + file_name.Substring(srcDir.Length), true);
                 
            }
            
            //System.IO.FileStream; 
            //   System.IO.FileStream.Dispose(Boolean disposing) ;
            //   System.IO.FileStream.Close();
           
           

            return newDir;
        }

        private void xcopy(string srcDir, string dstDir)
        {

        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            List<string> computerNames = new  List<string>();
            foreach (string computerName in listBox1.SelectedItems)
            {
                computerNames.Add(computerName);
            }

            string newDirName = "";
            foreach (string client in computerNames)
            {
                try
                {
                    string src = testfilepath;
                    string dst = @"\\" + client + "\\test\\";

                    
                  

                    //newDirName = copyTestDir(src, dst);
                    MessageBox.Show(newDirName);
                    //File.Copy(@src, dst);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }

            MessageBox.Show(testfilename);
            runTests(computerNames, @"C:\test\" + newDirName + @"\" + testfilename);
        }

        private void runTests(List<string> computerNames, string testExePath){
           

            
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\Users\donatas\Desktop\testRun.bat"))
            {
                foreach (string computerName in computerNames)
                {
                    string line = @"C:\PSTools\PsExec.exe -i \\" + computerName + @" -u asb\labclient -p kPu$27mLi python " + testExePath;
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
