using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.DirectoryServices;
using System.Diagnostics;
using System.Management.Automation;
using System.Management;
using System.IO;
using System.Threading;

namespace ServiceLibrary
{
    public class Service
    {
        private static Service service;

        public static Service getInstance()
        {
            if (service == null)
                service = new Service();
            return service;
        }

        private Service()
        {

        }

        //domain eg.: asb.local
        public static List<string> GetComputers(string domain, string username, string password)
        {
            List<string> ComputerNames = new List<string>();

            DirectoryEntry entry = new DirectoryEntry("LDAP://" + domain, username, password);
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

        public void ExecuteCommand(string command, bool waitForExit = false)
        {
            int exitCode;
            ProcessStartInfo processInfo;
            Process process;

            processInfo = new ProcessStartInfo("cmd.exe", "/c " + command);
            processInfo.CreateNoWindow = true;
            processInfo.UseShellExecute = false;
            // *** Redirect the output ***
            processInfo.RedirectStandardError = true;
            processInfo.RedirectStandardOutput = true;

            process = Process.Start(processInfo);
            if (waitForExit)
                process.WaitForExit();

            // *** Read the streams ***
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            exitCode = process.ExitCode;

            //MessageBox.Show("output>>" + (String.IsNullOrEmpty(output) ? "(none)" : output));
            //MessageBox.Show("error>>" + (String.IsNullOrEmpty(error) ? "(none)" : error));
            //MessageBox.Show("ExitCode: " + exitCode.ToString(), "ExecuteCommand");
            process.Close();
        }

        public void ExecuteCmdSimple(string command)
        {
            System.Diagnostics.Process.Start(command);
        }

        public void runWmi()
        {
            try
            {
                object[] theProcessToRun = { "notepad.exe" };
                ConnectionOptions theConnection = new ConnectionOptions();
                theConnection.Username = "labclient@asb";
                theConnection.Password = "kPu$27mLi";
                ManagementScope theScope = new ManagementScope("\\YLGW036496\\root\\cimv2", theConnection);
                ManagementClass theClass = new ManagementClass(theScope, new ManagementPath("Win32_Process"), new ObjectGetOptions());
                theClass.InvokeMethod("Create", theProcessToRun);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        public void bbdRun()
        {
            runCmd(@"C:\Users\donatas\AppData\Local\Temp\bbd.bat");
        }

        public void newRunCmd(string target)
        {
            string arguments = @"C:\Windows\notepad32";
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = @"C:\PSTools\PsExec.exe";
            startInfo.Arguments = arguments;
            Debug.WriteLine(arguments);
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardError = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.CreateNoWindow = false;
            Process process = new Process();
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            process.Close();
        }

        public void runCmd(string target)
        {
            string path = "";
            string testFileName = "";
            int index = target.LastIndexOf("\\");
            if (index > 0)
            {
                path = target.Substring(0, index); // or index + 1 to keep slash
                testFileName = target.Substring(index + 1, target.Length - (index + 1)); // or index + 1 to keep slash
            }

            Process proc = null;
            try
            {
                string targetDir = string.Format(@path);//this is where mybatch.bat lies
                proc = new Process();
                proc.StartInfo.WorkingDirectory = targetDir;
                proc.StartInfo.FileName = testFileName;
                proc.StartInfo.Arguments = string.Format("10");//this is argument
                proc.StartInfo.CreateNoWindow = false;
                //proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                proc.Start();
                proc.BeginOutputReadLine();
                proc.WaitForExit();
                
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception Occurred :{0},{1}", ex.Message, ex.StackTrace.ToString());
            }
        }

        public void execBatch(string target)
        {
            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            int timeout = 1000;
            int NO_MILLISECONDS_IN_A_SECOND = 100;
            int NO_SECONDS_IN_A_MINUTE = 100;

            proc.StartInfo.FileName = target;

            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.UseShellExecute = false;

            proc.Start();

            proc.WaitForExit
                (
                    (timeout <= 0)
                        ? int.MaxValue : timeout * NO_MILLISECONDS_IN_A_SECOND *
                            NO_SECONDS_IN_A_MINUTE
                );

            string errorMessage = proc.StandardError.ReadToEnd();
            proc.WaitForExit();

            string outputMessage = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();
        }

        public void ExecuteCommandNoOutput(string command, bool waitForExit = false)
        {
            //int exitCode;
            ProcessStartInfo processInfo;
            Process process;

            processInfo = new ProcessStartInfo("cmd.exe", "/c " + command);
            processInfo.CreateNoWindow = true;
            processInfo.UseShellExecute = false;
            // *** Redirect the output ***
            //processInfo.RedirectStandardError = true;
            //processInfo.RedirectStandardOutput = true;

            process = Process.Start(processInfo);
            if (waitForExit)
                process.WaitForExit();

            // *** Read the streams ***
            //string output = process.StandardOutput.ReadToEnd();
            //string error = process.StandardError.ReadToEnd();

            // exitCode = process.ExitCode;

            //MessageBox.Show("output>>" + (String.IsNullOrEmpty(output) ? "(none)" : output));
            //MessageBox.Show("error>>" + (String.IsNullOrEmpty(error) ? "(none)" : error));
            //MessageBox.Show("ExitCode: " + exitCode.ToString(), "ExecuteCommand");
            process.Close();
        }

        private void KillProcThread(string computerName, string processName)
        {
            Debug.WriteLine(computerName + " " + processName);
            Debug.WriteLine("Killing begins >:)");
            var LocalPassword = "kPu$27mLi";
            var ssLPassword = new System.Security.SecureString();
            foreach (char c in LocalPassword)
                ssLPassword.AppendChar(c);

            PSCredential Credential = new PSCredential("labclient@asb", ssLPassword);
            string serverName = computerName;
            string cmdlet = "Taskkill /IM " + processName + " /F";
            using (PowerShell powershell = PowerShell.Create())
            {
                powershell.AddCommand("Set-Variable");
                powershell.AddParameter("Name", "cred");
                powershell.AddParameter("Value", Credential);

                powershell.AddScript(@"$s = New-PSSession -ComputerName '" + serverName + "' -Credential $cred");
                powershell.AddScript(@"$a = Invoke-Command -Session $s -ScriptBlock { " + cmdlet + " }");
                powershell.AddScript(@"Remove-PSSession -Session $s");
                powershell.AddScript(@"echo $a");

                var results = powershell.Invoke();

                foreach (var item in results)
                {
                    Debug.WriteLine(item);
                }

                if (powershell.Streams.Error.Count > 0)
                {
                    Debug.WriteLine("{0} errors", powershell.Streams.Error.Count);
                }
            }
        }

        public void killRemoteProcess(string computerName, string processName)
        {
            new Thread(() => KillProcThread(computerName, processName)).Start();
        }

        public IEnumerable<string> GetFiles(string path)
        {
            Queue<string> queue = new Queue<string>();
            queue.Enqueue(path);
            while (queue.Count > 0)
            {
                path = queue.Dequeue();
                try
                {
                    foreach (string subDir in Directory.GetDirectories(path))
                    {
                        queue.Enqueue(subDir);
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);
                }
                string[] files = null;
                try
                {
                    files = Directory.GetFiles(path);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);
                }
                if (files != null)
                {
                    for (int i = 0; i < files.Length; i++)
                    {
                        yield return files[i];
                    }
                }
            }
        }
    }
}
