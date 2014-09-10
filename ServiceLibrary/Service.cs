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
using System.Net.NetworkInformation;

namespace ServiceLibrary
{
    public class Service
    {
        private static Service service;

        public Credentials Credentials { get; set; }
        private User user = null;

        //private readonly string sharedNetworkTempFolder = @"\\Win2008\shared\";
        private readonly string sharedNetworkTempFolder = @"\\asb.local\staff\users\labclient\";
        private readonly string inputBlockApp = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "InputBlocker", "InputBlocker.exe");
        private static readonly string testFolder = @"C:\Cobe Lab\";
        private static readonly string clientsFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"clients.ini");
        private static readonly string authFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"auth.ini");
        private readonly string tempPath = System.IO.Path.GetTempPath();

        private List<WindowSize> windowSizes = new List<WindowSize>();
        public List<WindowSize> WindowSizes { get { return windowSizes; } }

        private bool AppActive { get; set; }
        public event EventHandler ProgressUpdate;
        public readonly object key = new object();

        private ScreenShare screenShare = ScreenShare.getInstance();

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
                    string domainName = file.ReadLine();
                    string userName = file.ReadLine();
                    string userPassword = file.ReadLine();

                    Credentials = new Credentials(domainName, userName, userPassword);
                    AppActive = true;

                    windowSizes.Add(new WindowSize("Full Screen", null, null));
                    windowSizes.Add(new WindowSize("Half Screen Left", 960, 1080));
                    WindowSize size = new WindowSize("Half Screen Right", 960, 1080);
                    size.XPos = 960;
                    size.YPos = 0;
                    windowSizes.Add(size);
                }
            }
            catch (FileNotFoundException ex)
            {
                throw new FileNotFoundException("auth.ini", ex);
            }
        }

        public void StopAndClean()
        {
            AppActive = false;
        }

        public void StartPingSvc(List<LabClient> clients)
        {
            new Thread(delegate()
               {
                   while (AppActive)
                   {
                       foreach (LabClient client in clients)
                       {
                           Thread t = new Thread(delegate()
                           {
                               bool success = false;
                               Ping ping = new Ping();
                               try
                               {
                                   PingReply pingReply = ping.Send(client.ComputerName);
                                   if (pingReply.Status == IPStatus.Success)
                                   {
                                       success = true;
                                   }
                               }
                               catch (Exception ex)
                               {
                                   Debug.WriteLine(ex.Message);
                               }

                               client.Active = success;
                           });
                           t.IsBackground = true;
                           t.Start();
                       }
                       //check again after x sec or interrupt if app is stopped 
                       lock (key)
                       {
                           Monitor.Wait(key, new TimeSpan(0, 0, 30));
                       }
                   }
               }).Start();
        }

        /// <summary>
        /// Reads the clients.txt into the program for a list of computers in a specific lab.
        /// </summary>
        /// <returns>List of clients</returns>

        public List<LabClient> GetLabComputersFromStorage()
        {
            List<LabClient> clientlist = new List<LabClient>();
            using (System.IO.StreamReader file = new System.IO.StreamReader("clients.txt"))
            {
                int roomNo;
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
                    roomNo = Int32.Parse(data[0]);
                    boothNo = Int32.Parse(data[1]);
                    compname = data[2];
                    ip = data[3];
                    mac = data[4];
                    Debug.WriteLine(boothNo);
                    Debug.WriteLine(compname);
                    Debug.WriteLine(ip);
                    Debug.WriteLine(mac);

                    LabClient client = new LabClient(roomNo, compname, boothNo, mac, ip);
                    clientlist.Add(client);
                }
            }

            return clientlist;
        }

        public List<LabClient> filterForRoom(List<LabClient> clients, int roomNo)
        {
            List<LabClient> newClients = new List<LabClient>();
            foreach (LabClient client in clients)
            {
                if (client.RoomNo == roomNo)
                    newClients.Add(client);
            }
            return newClients;
        }

        /// <summary>
        /// Downloads the bridge's list of computers which have MAC and booth number.
        /// Then connects MACs to IP-s using local ARP pool, and looks up computer names using NBTSTAT from IP address.
        /// Throws exception if ARP list is not filled up sufficiently or the bridge's client list cannot be downloaded.
        /// </summary>
        /// <returns>List of clients</returns>
        public List<LabClient> GetLabComputersNew2(int labNo)
        {
            List<LabClient> clientlist = new List<LabClient>();
            //Get MAC addresses from Bridge
            try
            {
                String contents = new System.Net.WebClient().DownloadString("http://10.204.77.17:8000/?downloadcfg=1");
                // Write bridge list to a file.
                System.IO.StreamWriter file0 = new System.IO.StreamWriter("bridgelist.txt");
                file0.WriteLine(contents);
                file0.Close();
            }
            catch (Exception ex)
            {
                throw new WebException("Error trying to reach the bridge's client list. Error:", ex);
            }


            //Get MAC addresses from list
            using (System.IO.StreamReader file = new System.IO.StreamReader("bridgelist.txt"))
            {
                int roomNo;
                int boothNo;
                string line;
                string mac;
                while (((line = file.ReadLine()) != null) && (line.Length > 10))
                {
                    roomNo = Int32.Parse(line.Substring(0, 1));
                    mac = line.Substring(4);
                    mac = mac.Replace(" ", String.Empty);
                    mac = mac.Replace(":", String.Empty);
                    mac = mac.Replace("\u0009", String.Empty);
                    boothNo = Int32.Parse(line.Substring(2, 2).Trim());
                    LabClient client = new LabClient(roomNo, "", boothNo, mac, "");
                    clientlist.Add(client);
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
                    clientListString += client.RoomNo + " " + client.BoothNo + " " + client.ComputerName + " " + client.Ip + " " + client.Mac + Environment.NewLine;
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

        public void LaunchCommandLineApp(string path, string arguments)
        {
            // Use ProcessStartInfo class
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = false;
            startInfo.UseShellExecute = false;
            startInfo.FileName = path;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.Arguments = arguments;

            try
            {
                // Start the process with the info we specified.
                // Call WaitForExit and then the using statement will close.
                using (Process exeProcess = Process.Start(startInfo))
                {
                    exeProcess.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
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
            process.Close();
            return output;

        }

        public void CopyFilesToNetworkShare(string srcDir) {
            //Copies a selected file to shared drive for distribution
            string copyPath = Path.Combine(tempPath, "localCopy.bat");
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(copyPath))
            {
                file.WriteLine("@echo off");
                string dstDir = @"\\BSSFILES2\Dept\adm\labrun\temp";
                string line = @"xcopy """ + srcDir + @""" """ + dstDir + @""" /V  /Y /Q";
                file.WriteLine(line);
            }
            Debug.WriteLine("tempPath");
            service.ExecuteCommandNoOutput(copyPath, true);
        }

        /// <summary>
        /// Transfers a file from the shared drive to each selected lab client.
        /// </summary>
        /// <returns>Nothing</returns>
        public void CopyFilesFromNetworkShareToClients(string srcPath, string fileName, List<LabClient> clients)
        {

            //
            foreach (LabClient client in clients)
            {
                string batFileName = Path.Combine(tempPath, "CustomCopy" + client.ComputerName + ".bat");
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(batFileName))
                {
                    file.WriteLine("@echo off");
                    // Embed xcopy command to transfer ON labclient FROM shared drive TO labclient
                    string copyCmd = @"xcopy """+@"\\BSSFILES2\Dept\adm\labrun\temp\" + fileName + ""+@""" ""C:\labrun\temp"" /V /Y /Q ";
                    // Deploy and run batfile FROM Server TO labclient using PSTools
                    string line = @"C:\PSTools\PsExec.exe -d -i 1 \\" + client.ComputerName + @" -u " + service.Credentials.DomainSlashUser + @" -p " + service.Credentials.Password + @" cmd /c (" + copyCmd + @")";
                    file.WriteLine(line);
                }
                service.StartNewCmdThread(batFileName);
            }
        }

        /// <summary>
        /// Transfers and runs a file from the shared drive to each selected lab client.
        /// </summary>
        /// <returns>Nothing</returns>
        public void CopyAndRunFilesFromNetworkShareToClients(string srcPath, string fileName, List<LabClient> clients)
        {

            //
            foreach (LabClient client in clients)
            {
                string batFileName = Path.Combine(tempPath, "CustomCopy" + client.ComputerName + ".bat");
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(batFileName))
                {
                    file.WriteLine("@echo off");
                    // Embed xcopy command to transfer ON labclient FROM shared drive TO labclient
                    string copyCmd = @"xcopy """ + @"\\BSSFILES2\Dept\adm\labrun\temp\" + fileName + "" + @""" ""C:\labrun\temp"" /V /Y /Q ";
                    // Run file on client after copied to local drive
                    string runCmd = @"""" + @"C:\labrun\temp\" + fileName + @"""";
                    // Deploy and run batfile FROM Server TO labclient using PSTools
                    string line = @"C:\PSTools\PsExec.exe -d -i 1 \\" + client.ComputerName + @" -u " + service.Credentials.DomainSlashUser + @" -p " + service.Credentials.Password + @" cmd /c (" + copyCmd + @" ^& " + runCmd + @")";
                    file.WriteLine(line);
                }
                service.StartNewCmdThread(batFileName);
            }
        }

        /// <summary>
        /// Deletes the files contained in supplied hashset on the supplied clients.
        /// </summary>
        /// <returns>Nothing</returns> 
        public void deleteFiles(HashSet<string> files, List<LabClient> clients)
        {
            //foreach (LabClient client in clients)
            //{
                
            //        string batFileName = Path.Combine(tempPath, "CustomCopy" + client.ComputerName + ".bat");
            //        using (System.IO.StreamWriter file = new System.IO.StreamWriter(batFileName))
            //        {
            //            file.WriteLine("@echo off");
            //            foreach (string fileLine in files)
            //            {
            //                string deleteCmd = @"delete C:\labrun\temp" + fileLine +" /Q ";
            //            }
            //            string line = @"C:\PSTools\PsExec.exe -d -i 1 \\" + client.ComputerName + @" -u " + service.Credentials.DomainSlashUser + @" -p " + service.Credentials.Password + @" cmd /c (" + copyCmd + @" ^& " + runCmd + @")";
            //            file.WriteLine(line);
            //        }
            //        service.StartNewCmdThread(batFileName);
            //}
        }

        /// <summary>
        /// Runs previously transferred file on selectedm clients.
        /// </summary>
        /// <returns>Nothing</returns> 
       public void RunCustomFileOnClients(List<LabClient> clients, string filename){
           foreach (LabClient client in clients){
               string batFileName = Path.Combine(tempPath, "CustomRun" + client.ComputerName + ".bat"); 
               using (System.IO.StreamWriter file = new System.IO.StreamWriter(batFileName))
               {
                   file.WriteLine("@echo off");
                   string runCmd = @"""" + @"C:\labrun\temp\" + filename + @"""";
                   string line = @"C:\PSTools\PsExec.exe -d -i 1 \\" + client.ComputerName + @" -u " + service.Credentials.DomainSlashUser + @" -p " + service.Credentials.Password + @" cmd /c (" + runCmd + @")";
                   file.WriteLine(line);
               }
               service.StartNewCmdThread(batFileName);
           }
       }

        public void InputDisable(List<LabClient> clients)
        {
            string blockerDirName = Path.GetFileName(Path.GetDirectoryName(inputBlockApp));

            //-----local copy
            string copyPath = Path.Combine(tempPath, "localCopy.bat");
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(copyPath))
            {
                file.WriteLine("@echo off");
                string srcDir = Path.GetDirectoryName(inputBlockApp);
                string dstDir = Path.Combine(service.SharedNetworkTempFolder, blockerDirName);
                string line = @"xcopy """ + srcDir + @""" """ + dstDir + @""" /V /E /Y /Q /I";
                file.WriteLine(line);
            }
            service.ExecuteCommandNoOutput(copyPath, true);
            //-----end

            //service.runRemoteProgram(compList, inputBlockApp);
            //---onecall to client: copy and run
            int i = 0;
            foreach (LabClient client in clients)
            {
                string copyPathRemote = Path.Combine(tempPath, "remoteCopyRun" + client.ComputerName + ".bat");
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(copyPathRemote))
                {
                    file.WriteLine("@echo off");

                    string srcDir = Path.Combine(service.SharedNetworkTempFolder, blockerDirName);
                    string dstDir = Path.Combine(service.TestFolder, Path.GetFileName(Path.GetDirectoryName(inputBlockApp)));
                    string copyCmd = @"xcopy """ + srcDir + @""" """ + dstDir + @""" /V /E /Y /Q /I";

                    string runLocation = Path.Combine(dstDir, Path.GetFileName(inputBlockApp));
                    string runCmd = @"start """" """ + runLocation + @"""";
                    string line = @"C:\PSTools\PsExec.exe -d -i 1 \\" + client.ComputerName + @" -u " + service.Credentials.DomainSlashUser + @" -p " + service.Credentials.Password + @" cmd /c (" + copyCmd + @" ^& " + runCmd + @")";

                    file.WriteLine(line);
                }
                service.StartNewCmdThread(copyPathRemote);
                i++;
            }

            //-----notify ui
            service.notifyStatus("Input Disable Request Sent");
            //-----end
        }

        public void RunRemotePSCmdLet(string computerName, string cmdLet)
        {
            Thread t = new Thread(delegate()
               {
                   var LocalPassword = Credentials.Password;
                   var ssLPassword = new System.Security.SecureString();
                   foreach (char c in LocalPassword)
                       ssLPassword.AppendChar(c);

                   PSCredential Credential = new PSCredential(Credentials.UserAtDomain, ssLPassword);

                   using (PowerShell powershell = PowerShell.Create())
                   {
                       powershell.AddCommand("Set-Variable");
                       powershell.AddParameter("Name", "cred");
                       powershell.AddParameter("Value", Credential);
                       
                       powershell.AddScript(@"$s = New-PSSession -ComputerName '" + computerName + "' -Credential $cred");
                       powershell.AddScript(@"$a = Invoke-Command -Session $s -ScriptBlock { " + cmdLet + " }");
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
               });
            t.IsBackground = true;
            t.Start();
        }

        public void InputEnable(List<LabClient> clients)
        {

            foreach (LabClient client in clients)
            {
                killRemoteProcess(client.ComputerName, "InputBlocker.exe");
                RunRemotePSCmdLet(client.ComputerName, @"remove-itemproperty -Path hkcu:software\microsoft\windows\currentversion\policies\system -Name ""DisableTaskMgr""");

                //-----notify ui
                notifyStatus("Input Enabled");
                //-----end
            }

        }

        public void runRemoteProgram(List<LabClient> compList, string path, string param = "")
        {
            foreach (LabClient client in compList)
            {
                string compName = client.ComputerName.ToString();


                string copyPathRemote = Path.Combine(tempPath, "remoteRun" + compName + ".bat");

                using (System.IO.StreamWriter file = new System.IO.StreamWriter(copyPathRemote))
                {
                    file.WriteLine("@echo off");
                    string runCmd = @"""" + path + @"""" + " " + param;
                    string line = @"C:\PSTools\PsExec.exe -d -i 1 \\" + compName + @" -u " + service.Credentials.DomainSlashUser + @" -p " + service.Credentials.Password + " " + runCmd;
                    file.WriteLine(line);
                }
                service.StartNewCmdThread(copyPathRemote);
            }
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

        public void CloseRemoteChrome(List<LabClient> computers)
        {
            Thread t = new Thread(() =>
            {
                string processName = "Chrome.exe";
                foreach (LabClient computer in computers)
                {
                    string cmdlet = @"Taskkill /IM " + processName + @" /F
                    $a = $env:LOCALAPPDATA + ""\Google\Chrome\User Data\Default\Preferences"" 
                    $content = get-content $a | % { $_ -replace '""exited_cleanly"": false', '""exited_cleanly"": true' } | % { $_ -replace '""exit_type"": ""Crashed""', '""exit_type"": ""Normal""' }
                    $content | set-content $a";
                    RunRemotePSCmdLet(computer.ComputerName, cmdlet);
                }
            });
            t.IsBackground = true;
            t.Start();
        }

        public void killRemoteProcess(string computerName, string processName)
        {
            new Thread(() => KillProcThread(computerName, processName)).Start();
        }

        private void KillProcThread(string computerName, string processName)
        {
            string cmdlet = "Taskkill /IM " + processName + " /F";
            RunRemotePSCmdLet(computerName, cmdlet);

            //-----notify ui
            notifyStatus("Task Kill Completed");
            //-----end
        }

        public void ShutdownComputers(List<LabClient> clients)
        {
            new Thread(() => ShutItThread(clients)).Start();
        }

        private void ShutItThread(List<LabClient> clients)
        {
            Debug.WriteLine("Shutting begins >:)");
            var LocalPassword = Credentials.Password;
            var ssLPassword = new System.Security.SecureString();
            foreach (char c in LocalPassword)
                ssLPassword.AppendChar(c);

            PSCredential Credential = new PSCredential(Credentials.UserAtDomain, ssLPassword);


            

            foreach (LabClient client in clients)
            {
                new Thread(delegate()
                {
                    using (PowerShell powershell = PowerShell.Create())
                    {
                        powershell.AddCommand("Set-Variable");
                        powershell.AddParameter("Name", "cred");
                        powershell.AddParameter("Value", Credential);

                        powershell.AddScript(@"$a = Stop-Computer -ComputerName " + client.ComputerName + @" -Force -Credential $cred");
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
                }).Start();
            }

            notifyStatus("Shutdown request sent");
            //-----end
        }

        public void notifyStatus(string msg)
        {
            //-----notify ui
            if (ProgressUpdate != null)
                ProgressUpdate(this, new StatusEventArgs(msg));
            //-----end
        }

        public Thread TransferAndRun(List<LabClient> selectedClients)
        {
            var t = new Thread(() => ScrVwrCopyAndRun(selectedClients));
            t.Start();
            return t;
        }

        private void ScrVwrCopyAndRun(List<LabClient> selectedClients)
        {
            //Run viewer on each selected lab client.
            foreach (LabClient client in selectedClients)
            {
                string batFileName = Path.Combine(tempPath, "ScrViewer_" + client.ComputerName + ".bat");
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(batFileName))
                {
                    file.WriteLine("@echo off");


                    string copyCmd = @"xcopy ""\\BSSFILES2\dept\adm\labrun\scr-viewer"" ""C:\labrun\scr-viewer"" /V /E /Y /Q /I";
                    string runCmd = @"""" + @"C:\labrun\scr-viewer\scr-viewer.exe" + @"""";
                    string line = @"C:\PSTools\PsExec.exe -d -i 1 \\" + client.ComputerName + @" -u " + service.Credentials.DomainSlashUser + @" -p " + service.Credentials.Password + @" cmd /c (" + copyCmd + @" ^& " + runCmd + @")";
                    file.WriteLine(line);
                }
                service.StartNewCmdThread(batFileName);
            }
        }

        public void NetDisable(List<LabClient> clients)
        {
            string script = @"function Add-FirewallRule {
                $fw = New-Object -ComObject hnetcfg.fwpolicy2 
                $rule = New-Object -ComObject HNetCfg.FWRule
        
                $appName = $null,
                $serviceName = $null

                $rule.Name = ""Block http(s) ports""
                if ($appName -ne $null) { $rule.ApplicationName = $appName }
                if ($serviceName -ne $null) { $rule.serviceName = $serviceName }
                $rule.Protocol = 6 #NET_FW_IP_PROTOCOL_TCP
                $rule.RemotePorts = ""80,443"" 
                $rule.Enabled = $true
                $rule.Grouping = ""@firewallapi.dll,-23255""
                $rule.Profiles = 7 # all
                $rule.Action = 0 # NET_FW_ACTION_ALLOW
                $rule.EdgeTraversal = $false
                $rule.Direction = 2
                $fw.Rules.Add($rule)
            }
Add-FirewallRule
";
            foreach (LabClient client in clients)
            {
                RunRemotePSCmdLet(client.ComputerName, script);

                //-----notify ui
                if (ProgressUpdate != null)
                    ProgressUpdate(this, new StatusEventArgs("Net Access (http(s)) was disabled!"));
                //-----end
            }
        }

        public void NetEnable(List<LabClient> clients)
        {
            string script = @"$fw = New-Object -ComObject hnetcfg.fwpolicy2 
                            $fw.Rules.Remove(""Block http(s) ports"")";

            foreach (LabClient client in clients)
            {
                RunRemotePSCmdLet(client.ComputerName, script);

                //-----notify ui
                if (ProgressUpdate != null)
                    ProgressUpdate(this, new StatusEventArgs("Net Access (http(s)) was enabled!"));
                //-----end
            }
        }

        public void StartScreenSharing(List<LabClient> clients)
        {
            screenShare.Start(clients);
        }

        public void StopScreenSharing(List<LabClient> clients)
        {
            screenShare.Stop(clients);
        }

        public User Login(string username, string password)
        {
            User user = null;
            WebClient webClient = new WebClient();
            string userHash = webClient.DownloadString("https://cobelab.au.dk/modules/StormDb/extract/login?username=" + username + "&password=" + password);
            if (userHash.Contains("templogin"))
            {
                user = new User(username, password);
                user.UniqueHash = userHash;
            }
            this.user = user;
            return user;
        }

        public void LogOut()
        {
            user = null;
        }

        public bool LoggedIn()
        {
            return user != null;
        }

        public List<string> GetProjects()
        {
            WebClient webClient = new WebClient();
            string projectsStr = webClient.DownloadString("https://cobelab.au.dk/modules/StormDb/extract/projects?" + user.UniqueHash);
            string[] lines = projectsStr.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
            return new List<string>(lines); ;
        }
    }
}