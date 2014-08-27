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
using System.Collections;
using System.Text.RegularExpressions;
using System.Net;
using System.Windows.Forms;


namespace ServiceLibrary
{
    public class Service
    {
        public Credentials Credentials { get; set; }
        private readonly string domainName;
        private readonly string userName;
        private readonly string userPassword;
        private readonly string domainSlashUser;
        private readonly string userAtDomain;
        private readonly string sharedNetworkTempFolder = @"\\asb.local\staff\users\labclient\test\";
        //private readonly string sharedNetworkTempFolder = @"\\Win2008\shared\";
        private static readonly string testFolder = @"C:\test\";
        private static readonly string clientsFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"clients.ini");
        private static readonly string authFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"auth.ini");

        private readonly string tempPath = System.IO.Path.GetTempPath();
        private List<WindowSize> windowSizes = new List<WindowSize>();
        public List<WindowSize> WindowSizes { get { return windowSizes; } }

        private static Service service;
        public event EventHandler ProgressUpdate;

        public static Service getInstance()
        {
            if (service == null)
                service = new Service();
            return service;
        }

        public string TestFolder
        {
            get { return testFolder; }
        }

        public string DomainSlashUser
        {
            get { return domainSlashUser; }
        }

        public string TempPath
        {
            get { return tempPath; }
        }

        public string SharedNetworkTempFolder
        {
            get { return sharedNetworkTempFolder; }
        }

        private Service()
        {
            try
            {
                using (System.IO.StreamReader file = new System.IO.StreamReader(authFile))
                {
                    domainName = file.ReadLine();
                    userName = file.ReadLine();
                    userPassword = file.ReadLine();

                    domainSlashUser = domainName + @"\" + userName;
                    userAtDomain = userName + @"@" + domainName;
                    Credentials = new Credentials(domainName, userName, userPassword);

                    windowSizes.Add(new WindowSize("Full Screen", null, null));
                    windowSizes.Add(new WindowSize("Half Screen", 960, 1080));
                    //windowSizes.Add(new WindowSize("Whatever Screen", null, null));
                }
            }
            catch (FileNotFoundException ex)
            {
                throw new FileNotFoundException("auth.ini", ex);
            }
        }



        public List<LabClient> GetLabComputersFromStorage()
        {

            List<LabClient> clientlist = new List<LabClient>();
            using (System.IO.StreamReader file = new System.IO.StreamReader("clients.txt"))
            {
                int boothNo;
                string line;
                string mac;
                string compname;
                string ip;
                while (((line = file.ReadLine()) != null) && (line.Length > 3))
                {
                    string[] data = line.Split(null);
                    foreach (string temp in data)
                    {
                        Debug.WriteLine(temp);
                    }
                    boothNo = Int32.Parse(data[0]);
                    compname = data[1];
                    ip = data[2];
                    mac = data[3];
                    Debug.WriteLine(boothNo);
                    Debug.WriteLine(compname);
                    Debug.WriteLine(ip);
                    Debug.WriteLine(mac);

                    LabClient client = new LabClient(compname, boothNo, mac, ip);
                    clientlist.Add(client);

                }
            }

            return clientlist;
        }

        /// <summary>
        /// Downloads the bridge's list of computers which have MAC and booth number.
        /// Then connects MACs to IP-s using ARP and checks computer names using IP.
        /// Throws exception if ARP list is not filled up sufficiently.
        /// </summary>
        /// <returns>List of clients</returns>


        public List<LabClient> GetLabComputersNew2()
        {

            List<LabClient> clientlist = new List<LabClient>();
            //Get MAC addresses from Bridge
            String contents = new System.Net.WebClient().DownloadString("http://10.204.77.17:8000/?downloadcfg=1");
            // Write it to a file.
            System.IO.StreamWriter file0 = new System.IO.StreamWriter("bridgelist.txt");
            file0.WriteLine(contents);
            file0.Close();

            //Get MAC addresses from list
            using (System.IO.StreamReader file = new System.IO.StreamReader("bridgelist.txt"))
            {
                int boothNo;
                string line;
                string mac;
                while (((line = file.ReadLine()) != null) && (line.Length > 10))
                {
                    if (Int32.Parse(line.Substring(0, 1)) != 2)
                    {
                        mac = line.Substring(4);
                        mac = mac.Replace(" ", String.Empty);
                        mac = mac.Replace(":", String.Empty);
                        mac = mac.Replace("\u0009", String.Empty);
                        boothNo = Int32.Parse(line.Substring(2, 2).Trim());
                        LabClient client = new LabClient("", boothNo, mac, "");
                        clientlist.Add(client);
                    }
                }
            }

            //Get local ARP list to match MAC addresses to IP-s
            Process p = new Process();
            p.StartInfo = new ProcessStartInfo(@"C:\Windows\System32\arp.exe", "/a") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, RedirectStandardInput = true, CreateNoWindow = true };
            p.Start();

            //Store the output to a string
            String Contents = p.StandardOutput.ReadToEnd();

            // Write the string to a file.
            System.IO.StreamWriter file2 = new System.IO.StreamWriter("arp.txt");
            file2.WriteLine(Contents);
            file2.Close();

            //Process file
            using (System.IO.StreamReader file = new System.IO.StreamReader("arp.txt"))
            {
                string line;
                string mac = "";
                string ip = "";
                int i = 1;
                int cutLines = 3;

                while ((line = file.ReadLine()) != null)
                {
                    if (i <= cutLines)
                    {
                        i++;
                    }
                    else
                    {
                        if (line.Length > 20)
                        {
                            ip = line.Substring(0, 17);
                            ip = ip.Trim();
                            mac = line.Substring(17, 25);
                            mac = mac.Replace(" ", String.Empty);
                            mac = mac.Replace("-", String.Empty);
                            mac = mac.Replace("\u0009", String.Empty);
                        }

                        //Check for matching MACs, if found, update list of clients with IP
                        foreach (LabClient client in clientlist)
                        {
                            if (client.Mac == mac)
                            {
                                client.Ip = ip;

                            }

                        }

                    }
                }


                //Get computer names, match with IP using NBTSTAT

                foreach (LabClient client in clientlist)
                {

                    String Contents5 = ExecuteCommand("nbtstat.exe -a " + client.Ip, true);
                    if (Contents5.IndexOf("<00>  UNIQUE") == -1)
                    {
                        throw new Exception();
                    }
                    String until = Contents5.Substring(0, Contents5.IndexOf("<00>  UNIQUE")).Trim();
                    string[] stringArray = until.Split(null);
                    string name = "";
                    foreach (string str in stringArray)
                    {
                        name = str;
                    }
                    client.ComputerName = name;


                }

                //Write clientlist to file for testing
                string clientListString = "";
                foreach (LabClient client in clientlist)
                {
                    clientListString += client.BoothNo + " " + client.ComputerName + " " + client.Ip + " " + client.Mac + Environment.NewLine;
                    System.IO.StreamWriter fileClients = new System.IO.StreamWriter("clients.txt");
                    fileClients.WriteLine(clientListString);
                    fileClients.Close();
                }
            }
            return clientlist;

        }

        public void DNSLookup(string hostNameOrAddress)
        {
            Debug.WriteLine("Lookup: {0}\n", hostNameOrAddress);

            IPHostEntry hostEntry = Dns.GetHostEntry(hostNameOrAddress);
            Console.WriteLine("  Host Name: {0}", hostEntry.HostName);

            IPAddress[] ips = hostEntry.AddressList;
            foreach (IPAddress ip in ips)
            {
                Console.WriteLine("  Address: {0}", ip);
            }

            Debug.WriteLine("");
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

        public String ExecuteCommand(string command, bool waitForExit = false)
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
            return output;
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
                proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
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

        public void ProcessStartSimple(string path)
        {
            Process.Start(path);
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

        public void killRemoteProcess(string computerName, string processName)
        {
            new Thread(() => KillProcThread(computerName, processName)).Start();
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

            //-----notify ui
            if (ProgressUpdate != null)
                ProgressUpdate(this, new StatusEventArgs("Task Kill Completed"));
            //-----end
        }

        public void ShutdownComputer(List<string> computerNames)
        {
            new Thread(() => ShutItThread(computerNames)).Start();
        }

        private void ShutItThread(List<string> computerNames)
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

            //-----notify ui
            if (ProgressUpdate != null)
                ProgressUpdate(this, new StatusEventArgs("Shutdown request sent"));
            //-----end
        }

        public void notifyStatus(string msg)
        {
            //-----notify ui
            if (ProgressUpdate != null)
                ProgressUpdate(this, new StatusEventArgs(msg));
            //-----end
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
