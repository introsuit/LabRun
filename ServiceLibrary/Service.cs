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
using System.Reflection;

namespace ServiceLibrary
{
    public class Service
    {
        private readonly string domainName;
        private readonly string userName;
        private readonly string userPassword;
        private readonly string domainSlashUser;
        private readonly string userAtDomain;
        private static readonly string resultFolder = @"C:\Dump\";
        private static readonly string clientsFile = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"clients.ini");
        private static readonly string authFile = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"auth.ini");

        private static readonly string tempPath = System.IO.Path.GetTempPath();

        private static Service service;

        public static Service getInstance()
        {
            if (service == null)
                service = new Service();
            return service;
        }

        private Service()
        {
            using (System.IO.StreamReader file = new System.IO.StreamReader(authFile))
            {
                domainName = file.ReadLine();
                userName = file.ReadLine();
                userPassword = file.ReadLine();

                domainSlashUser = domainName + @"\" + userName;
                userAtDomain = userName + @"@" + domainName;
            }
        }

        //domain eg.: asb.local
        //can throw exception
        public List<LabClient> GetLabComputersNew()
        {
            //DirectoryEntry entry = new DirectoryEntry("LDAP://OU=BSS Lab,OU=BSS Lab,OU=Computers,OU=Public,OU=Staff,DC=asb,DC=local");
            //DirectorySearcher mySearcher = new DirectorySearcher(entry);
            //mySearcher.Filter = ("(objectClass=computer)");
            //mySearcher.SizeLimit = int.MaxValue;
            //mySearcher.PageSize = int.MaxValue;

            List<LabClient> computerNames = new List<LabClient>();

            //foreach (SearchResult resEnt in mySearcher.FindAll())
            //{
            //    //"CN=SGSVG007DC"
            //    string ComputerName = resEnt.GetDirectoryEntry().Name;
            //    if (ComputerName.StartsWith("CN="))
            //        ComputerName = ComputerName.Remove(0, "CN=".Length);
            //    computerNames.Add(new LabClient(ComputerName, null));
            //}

            //mySearcher.Dispose();
            //entry.Dispose();

            //----------
            HashSet<LabClient> computers = new HashSet<LabClient>();
            List<LabClient> clientsInFile = new List<LabClient>();
            string cc = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "clients.ini");
            Debug.WriteLine(Assembly.GetExecutingAssembly().Location);
            string dd = @"D:\Documents\Visual Studio 2010\Projects\LabRun\LabRun\bin\Debug\clients.ini";
            Debug.WriteLine(dd);
            using (System.IO.StreamReader file = new System.IO.StreamReader(cc))
            {
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    line = line.Trim();

                    //ignore comments and empty lines
                    if (line.StartsWith(@"#") || line == "")
                    {
                        continue;
                    }

                    //split on whitespace+
                    string[] compData = System.Text.RegularExpressions.Regex.Split(line, @"\s{1,}");

                    string name = compData[0];
                    int? boothNo = null;

                    //set booth no. if defined
                    if (compData.Length > 1)
                        boothNo = Convert.ToInt32(compData[1]);
                    LabClient client = new LabClient(compData[0], boothNo);
                    computers.Add(client);
                    clientsInFile.Add(client);
                }
            }
            //-----------------
            foreach (LabClient compName in computerNames)
            {
                computers.Add(compName);
            }

            List<LabClient> clients = computers.ToList();

            //gets undefined clients from domain and adds to .ini file
            List<LabClient> differenceQuery =
                clients.Except(clientsInFile).ToList<LabClient>();
            updateClientsFile(differenceQuery);
            return clients;
        }

        private void updateClientsFile(List<LabClient> clients)
        {
            if (clients.Count == 0)
                return;

            using (System.IO.StreamWriter file = File.AppendText(clientsFile))
            {
                file.WriteLine(@"");
                file.WriteLine(@"#autogenerated");
                foreach (LabClient client in clients)
                {
                    file.WriteLine(client.ComputerName + " " + client.BoothNo);
                }
            }
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
                theConnection.Username = userName + @"@" + domainName;
                theConnection.Password = userPassword;
                ManagementScope theScope = new ManagementScope("\\YLGW036496\\root\\cimv2", theConnection);
                ManagementClass theClass = new ManagementClass(theScope, new ManagementPath("Win32_Process"), new ObjectGetOptions());
                theClass.InvokeMethod("Create", theProcessToRun);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
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

        public void xcopyPsychoPy(string srcDir, string dstDir, List<string> selectedClients)
        {
            string copyPath = tempPath + "testCopy.bat";
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(copyPath))
            {
                file.WriteLine("@echo off");
                foreach (string computerName in selectedClients)
                {
                    file.WriteLine(@"net use ""\\" + computerName + @"\test"" " + userPassword + @" /user:" + @domainSlashUser);

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

        public void runPsychoPyTests(List<string> computerNames, string testExePath)
        {
            int i = 0;
            foreach (string computerName in computerNames)
            {
                string runPath = tempPath + "testRun" + i + ".bat";
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(runPath))
                {
                    string line = @"C:\PSTools\PsExec.exe -d -i 1 \\" + computerName + @" -u " + domainSlashUser + @" -p " + userPassword +@" python " + testExePath;
                    file.WriteLine(line);
                }

                //MessageBox.Show(runPath);
                StartNewCmdThread(runPath);

                i++;
            }
        }

        public void xcopyPsychoPyResults(string srcWithoutComputerName, string dstFolderName, List<string> selectedClients)
        {
            string copyPath = tempPath + "testCopyResults.bat";
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(copyPath))
            {
                file.WriteLine("@echo off");
                foreach (string computerName in selectedClients)
                {
                    file.WriteLine(@"net use ""\\" + computerName + @"\test"" " + userPassword + @" /user:" + @domainSlashUser);

                    file.WriteLine(":copy");
                    string src = @"\\" + computerName + srcWithoutComputerName;
                    string dst = resultFolder + computerName + @"\" + dstFolderName;
                    string line = @"xcopy """ + src + @"\*.psydat"" """ + dst + @""" /V /E /Y /Q /I";
                    file.WriteLine(line);
                    line = @"xcopy """ + src + @"\*.csv"" """ + dst + @""" /V /E /Y /Q /I";
                    file.WriteLine(line);
                    line = @"xcopy """ + src + @"\*.log"" """ + dst + @""" /V /E /Y /Q /I";
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

        private void KillProcThread(string computerName, string processName)
        {
            Debug.WriteLine(computerName + " " + processName);
            Debug.WriteLine("Killing begins >:)");
            var LocalPassword = userPassword;
            var ssLPassword = new System.Security.SecureString();
            foreach (char c in LocalPassword)
                ssLPassword.AppendChar(c);

            PSCredential Credential = new PSCredential(userAtDomain, ssLPassword);
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

        public void ShutdownComputer(List<string> computerNames)
        {
            Debug.WriteLine("Shutting begins >:)");
            var LocalPassword = userPassword;
            var ssLPassword = new System.Security.SecureString();
            foreach (char c in LocalPassword)
                ssLPassword.AppendChar(c);

            PSCredential Credential = new PSCredential(userAtDomain, ssLPassword);

            string compList = "";
            foreach (string comp in computerNames)
            {
                compList += comp + ", ";
            }
            compList = compList.Substring(0, compList.Length - 2);
            Debug.WriteLine(compList);

            using (PowerShell powershell = PowerShell.Create())
            {
                powershell.AddCommand("Set-Variable");
                powershell.AddParameter("Name", "cred");
                powershell.AddParameter("Value", Credential);

                powershell.AddScript(@"$s = New-PSSession -ComputerName '" + compList + "' -Credential $cred");

                //string cmdlet = @"shutdown.exe -t 10 -f -s -c 'My comments'";
                //powershell.AddScript(@"$a = Invoke-Command -Session $s -ScriptBlock { " + cmdlet + " }");

                //cmdlet = @"Start-Sleep 5";
                //powershell.AddScript(@"$a = Invoke-Command -Session $s -ScriptBlock { " + cmdlet + " }");
                powershell.AddScript(@"$a = Stop-Computer -ComputerName " + compList + @" -Force -Credential $cred");

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

                foreach (ErrorRecord err in powershell.Streams.Error)
                {
                    Debug.WriteLine(err.ErrorDetails);
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
