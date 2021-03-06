﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management.Automation;
using System.IO;
using System.Threading;
using System.Reflection;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace ServiceLibrary
{
    public class Service
    {
        private static Service service;

        public Credentials Credentials { get; set; }
        private User user = null;
        public User User { get { return this.user; } }

        private readonly string sharedNetworkTempFolder = @"\\asb.local\staff\users\labclient\";
        private readonly string inputBlockApp = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "InputBlocker", "InputBlocker.exe");
        private static readonly string testFolder = @"C:\Cobe Lab\";
        private static readonly string clientsFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"clients.ini");
        private static readonly string authFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"auth.ini");
        private static readonly string configFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"config.ini");
        private static readonly string pythonLaunch = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"run_with_logs.py");
        private readonly string tempPath = System.IO.Path.GetTempPath();
        public string ResultsFolderName { get; set; }

        private List<WindowSize> windowSizes = new List<WindowSize>();
        public List<WindowSize> WindowSizes { get { return windowSizes; } }

        private bool AppActive { get; set; }
        public event EventHandler ProgressUpdate;
        public readonly object key = new object();

        private List<string> projects = new List<string>();

        private ScreenShare screenShare = ScreenShare.getInstance();
        public Config Config;

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

                    ResultsFolderName = "Results";
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

            if (!File.Exists(configFile))
            {
                throw new FileNotFoundException("config.ini");
            }
            else
            {
                Config = new Config(configFile);
            }

            if (!File.Exists(configFile))
            {
                throw new FileNotFoundException("config.ini");
            }
            else
            {
                Config = new Config(configFile);
            }
        }
        public bool FileExists()
        {
            return File.Exists(pythonLaunch);
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

        public void CopyFilesToNetworkShare(string srcDir, string timestamp)
        {
            //Copies a selected file to shared drive for distribution
            string copyPath = Path.Combine(tempPath, "localCopy.bat");
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(copyPath))
            {
                file.WriteLine("@echo off");
                string dstDir = @"\\BSSFILES2\Dept\adm\labrun\temp\Custom Run\" + timestamp + @"\Custom Run\";
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
        public void CopyFilesFromNetworkShareToClients(string srcPath, string fileName, List<LabClient> clients, string timestamp)
        {
            //
            foreach (LabClient client in clients)
            {
                string batFileName = Path.Combine(tempPath, "CustomCopy" + client.ComputerName + ".bat");
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(batFileName))
                {
                    file.WriteLine("@echo off");
                    string line = @"cmdkey.exe /add:" + client.ComputerName + @" /user:" + service.Credentials.DomainSlashUser + @" /pass:" + service.Credentials.Password;
                    file.WriteLine(line);

                    // Embed xcopy command to transfer ON labclient FROM shared drive TO labclient
                    string copyCmd = @"xcopy """ + @"\\BSSFILES2\Dept\adm\labrun\temp\Custom Run\" + timestamp + @"\Custom Run\" + fileName + "" + @""" ""C:\Cobe Lab\Custom Run\" + timestamp + @"\Custom Run\" + @""" /V /Y /Q ";
                    // Deploy and run batfile FROM Server TO labclient using PSTools
                    line = @"C:\PSTools\PsExec.exe -d -i 1 \\" + client.ComputerName + @" -u " + service.Credentials.DomainSlashUser + @" -p " + service.Credentials.Password + @" cmd /c (" + copyCmd + @")";
                    file.WriteLine(line);
                }
                service.StartNewCmdThread(batFileName);
            }
        }

        /// <summary>
        /// Transfers and runs a file from the shared drive to each selected lab client.
        /// </summary>
        /// <returns>Nothing</returns>
        public void CopyAndRunFilesFromNetworkShareToClients(string srcPath, string fileName, List<LabClient> clients, string param, string timestamp)
        {
            foreach (LabClient client in clients)
            {
                string batFileName = Path.Combine(tempPath, "CustomCopy" + client.ComputerName + ".bat");
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(batFileName))
                {
                    file.WriteLine("@echo off");
                    string line = @"cmdkey.exe /add:" + client.ComputerName + @" /user:" + service.Credentials.DomainSlashUser + @" /pass:" + service.Credentials.Password;
                    file.WriteLine(line);

                    // Embed xcopy command to transfer ON labclient FROM shared drive TO labclient
                    string copyCmd = @"xcopy """ + @"\\BSSFILES2\Dept\adm\labrun\temp\Custom Run\" + timestamp + @"\Custom Run\" + fileName + "" + @""" ""C:\Cobe Lab\Custom Run\" + timestamp + @"\Custom Run\""  /V /Y /Q ";

                    // Run file on client after copied to local drive
                    string runCmd = "";
                    if (param == "")
                    {
                         runCmd = @"""" + @"C:\Cobe Lab\Custom Run\" + timestamp + @"\" + @"\Custom Run\" + fileName + @"""";
                    }
                    else 
                    {
                         runCmd = @"""" + @"C:\Cobe Lab\Custom Run\" + timestamp + @"\" + @"\Custom Run\" + fileName + @""" """ + param + @"""";
                    }

                    // Deploy and run batfile FROM Server TO labclient using PSTools
                    line = @"C:\PSTools\PsExec.exe -d -i 1 \\" + client.ComputerName + @" -u " + service.Credentials.DomainSlashUser + @" -p " + service.Credentials.Password + @" cmd /c (" + copyCmd + @" ^& " + runCmd + @")";
                    file.WriteLine(line);
                }
                service.StartNewCmdThread(batFileName);
            }
        }
      

        /// <summary>
        /// Runs previously transferred file on selectedm clients.
        /// </summary>
        /// <returns>Nothing</returns> 
        public void RunCustomFileOnClients(List<LabClient> clients, string filename, string timestamp)
        {
            foreach (LabClient client in clients)
            {
                string batFileName = Path.Combine(tempPath, "CustomRun" + client.ComputerName + ".bat");
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(batFileName))
                {
                    file.WriteLine("@echo off");
                    string line = @"cmdkey.exe /add:" + client.ComputerName + @" /user:" + service.Credentials.DomainSlashUser + @" /pass:" + service.Credentials.Password;
                    file.WriteLine(line);

                    string runCmd = @"""" + @"C:\Cobe Lab\Custom Run\" + timestamp + @"\" + @"\Custom Run\" + filename + @"""";
                    line = @"C:\PSTools\PsExec.exe -d -i 1 \\" + client.ComputerName + @" -u " + service.Credentials.DomainSlashUser + @" -p " + service.Credentials.Password + @" cmd /c (" + runCmd + @")";
                    file.WriteLine(line);
                }
                service.StartNewCmdThread(batFileName);
            }
        }

        public void InputDisable(List<LabClient> clients)
        {
            Thread t = new Thread(delegate()
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
                            string line = @"cmdkey.exe /add:" + client.ComputerName + @" /user:" + service.Credentials.DomainSlashUser + @" /pass:" + service.Credentials.Password;
                            file.WriteLine(line);

                            string srcDir = Path.Combine(service.SharedNetworkTempFolder, blockerDirName);
                            string dstDir = Path.Combine(service.TestFolder, Path.GetFileName(Path.GetDirectoryName(inputBlockApp)));
                            string copyCmd = @"xcopy """ + srcDir + @""" """ + dstDir + @""" /V /E /Y /Q /I";

                            string runLocation = Path.Combine(dstDir, Path.GetFileName(inputBlockApp));
                            string runCmd = @"start """" """ + runLocation + @"""";
                            line = @"C:\PSTools\PsExec.exe -d -i 1 \\" + client.ComputerName + @" -u " + service.Credentials.DomainSlashUser + @" -p " + service.Credentials.Password + @" cmd /c (" + copyCmd + @" ^& " + runCmd + @")";

                            file.WriteLine(line);
                        }
                        service.StartNewCmdThread(copyPathRemote);
                        i++;
                    }

                    //-----notify ui
                    service.notifyStatus("Input Disable Request Sent");
                    //-----end
                });
            t.IsBackground = true;
            t.Start();
        }

        public void RunRemotePSCmdLet(string computerName, string cmdLet, bool waitForFinish = false)
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
            if (waitForFinish)
                t.Join();
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

        public void runRemoteProgram(List<LabClient> compList, string path, string param = "", bool cmdWithQuotes = true)
        {
            foreach (LabClient client in compList)
            {
                string compName = client.ComputerName.ToString();
                string copyPathRemote = Path.Combine(tempPath, "remoteRun" + compName + ".bat");

                using (System.IO.StreamWriter file = new System.IO.StreamWriter(copyPathRemote))
                {
                    file.WriteLine("@echo off");
                    string line = @"cmdkey.exe /add:" + client.ComputerName + @" /user:" + service.Credentials.DomainSlashUser + @" /pass:" + service.Credentials.Password;
                    file.WriteLine(line);

                    string runCmd = path;
                    if (cmdWithQuotes)
                    {
                        runCmd = @"""" + runCmd + @"""";
                    }
                    runCmd = runCmd + " " + param;
                    line = @"C:\PSTools\PsExec.exe -d -i 1 \\" + compName + @" -u " + service.Credentials.DomainSlashUser + @" -p " + service.Credentials.Password + " " + runCmd;
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

        public void KillLocalProcess(string processName)
        {
            using (PowerShell powershell = PowerShell.Create())
            {
                powershell.AddScript(@"$a = Taskkill /IM " + processName + @" /F");
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

        public void RunInNewThread(ThreadStart ts)
        {
            Thread t = new Thread(ts);
            t.IsBackground = true;
            t.Start();
        }

        public void killRemoteProcess(string computerName, string processName, bool waitForFinish = false)
        {
            KillProcThread(computerName, processName, waitForFinish);
        }

        public void killRemoteProcess(List<LabClient> computers, string processName, bool waitForFinish = false)
        {
            foreach (LabClient client in computers)
            {
                service.killRemoteProcess(client.ComputerName, processName, waitForFinish);
            }

            //-----notify ui
            notifyStatus("Task Kill Completed");
            //-----end
        }

        public void KillProcThread(string computerName, string processName, bool waitForFinish = false)
        {
            string cmdlet = "Taskkill /IM " + processName + " /F";
            RunRemotePSCmdLet(computerName, cmdlet, waitForFinish);
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
                    string line = @"cmdkey.exe /add:" + client.ComputerName + @" /user:" + service.Credentials.DomainSlashUser + @" /pass:" + service.Credentials.Password;
                    file.WriteLine(line);

                    string copyCmd = @"xcopy ""\\BSSFILES2\dept\adm\labrun\scr-viewer"" ""C:\labrun\scr-viewer"" /V /E /Y /Q /I";
                    string runCmd = @"""" + @"C:\labrun\scr-viewer\scr-viewer.exe" + @"""";
                    line = @"C:\PSTools\PsExec.exe -d -i 1 \\" + client.ComputerName + @" -u " + service.Credentials.DomainSlashUser + @" -p " + service.Credentials.Password + @" cmd /c (" + copyCmd + @" ^& " + runCmd + @")";
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

        private string GetCurrentMachineIp()
        {
            IPHostEntry host;
            string localIP = "?";
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                }
            }
            return localIP;
        }

        //adds fw rule to labclients to allow psexec from given ip
        public void UpdateFirewallRules(List<LabClient> clients)
        {
            //-----notify ui
            if (ProgressUpdate != null)
                ProgressUpdate(this, new StatusEventArgs("Working..."));

            //gets admin id for admin pc unique fw rule
            string adminPcId = Config.AdminId;
            string ruleName = "PsExec" + adminPcId;

            //gets ip of this machine
            string ip = GetCurrentMachineIp();
            if (ip == "?")
                throw new Exception("Unable to retrieve IP of this machine!");

            ThreadStart threadStart = delegate()
            {
                //remove previous unused fw rules from all labclients
                string script = @"$ruleName = """ + ruleName + @"""
                            $fw = New-Object -ComObject hnetcfg.fwpolicy2 
                            foreach ($rule in ($fw.Rules | where-object {$_.name -eq $ruleName } )) {
                                $fw.Rules.Remove($ruleName)
                            }";
                clients.ForEach(client => RunRemotePSCmdLet(client.ComputerName, script, true));

                //add new fw rule
                string cmd = @"cmd /c (netsh AdvFirewall firewall add rule name=" + ruleName + @" dir=in action=allow protocol=TCP localport=RPC RemoteIP=" + ip + @" profile=domain,private program=%WinDir%\system32\services.exe service=any)";
                runRemoteProgram(clients, cmd, "", false);

                //-----notify ui
                if (ProgressUpdate != null)
                    ProgressUpdate(this, new StatusEventArgs("FW rules update request sent"));
            };
            RunInNewThread(threadStart);
        }

        public void NetEnable(List<LabClient> clients)
        {
            string script = @"$ruleName = ""Block http(s) ports""
                            $fw = New-Object -ComObject hnetcfg.fwpolicy2 
                            foreach ($rule in ($fw.Rules | where-object {$_.name -eq $ruleName } )) {
                                $fw.Rules.Remove($ruleName)
                            }";

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
            Dms dms = new Dms();
            User user = dms.Login(username, password);
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

        public void InitProjects()
        {
            Thread t = new Thread(() =>
                {
                    projects = new Dms().GetProjects();
                });
            t.IsBackground = true;
            t.Start();
        }

        public List<string> GetProjects()
        {
            return projects;
        }

        public bool LocalProjectExists(string projectName)
        {
            string path = Path.Combine(testFolder, ResultsFolderName, projectName);
            return Directory.Exists(path);
        }

        public void MoveProject(string oldProject, string newProject)
        {
            string resPath = Path.Combine(testFolder, ResultsFolderName);
            string oldPath = Path.Combine(resPath, oldProject);
            string newPath = Path.Combine(resPath, newProject);
            //if the new project already exists, rename it with its timestamp
            if (Directory.Exists(newPath))
            {
                string timestamp = String.Format("{0:yyyyMMdd_HHmmss}", Directory.GetLastWriteTime(newPath));
                Directory.Move(newPath, Path.Combine(resPath, newProject + "_" + timestamp));
            }
            //rename old projects name
            Directory.Move(oldPath, newPath);
        }

        /// <summary>
        /// Deletes the temp directory containing transferred files on the supplied clients.
        /// </summary>
        /// <returns>Nothing</returns> 
        public void deleteFiles(List<LabClient> clients)
        {
            foreach (LabClient client in clients)
            {
                string batFileName = Path.Combine(tempPath, "DeleteTemp" + client.ComputerName + ".bat");
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(batFileName))
                {
                    file.WriteLine("@echo off");
                    string line = @"cmdkey.exe /add:" + client.ComputerName + @" /user:" + service.Credentials.DomainSlashUser + @" /pass:" + service.Credentials.Password;
                    file.WriteLine(line);

                    string deleteCmd = @"rmdir " + @"""C:\Cobe Lab\Custom Run\""" + @" /S /Q";
                    line = @"C:\PSTools\PsExec.exe -d -i 1 \\" + client.ComputerName + @" -u " + service.Credentials.DomainSlashUser + @" -p " + service.Credentials.Password + @" cmd /c (" + deleteCmd + @")";
                    file.WriteLine(line);
                }
                service.StartNewCmdThread(batFileName);
            }
        }

        //returns timestamp in yyyyMMdd_HHmmss format
        public string GetCurrentTimestamp()
        {
            DateTime timeStamp = DateTime.Now;
            return String.Format("{0:yyyyMMdd_HHmmss}", timeStamp);
        }

        /// <summary>
        /// Transfers a folder first to the shared drive then to each selected labclient.
        /// Uses PSExec delegated batch files, running on each client.
        /// Then runs selected file using same PSExec batch.
        /// </summary>
        /// <returns>Nothing</returns>
        public void CopyAndRunFolder(List<LabClient> clients, string folderPath, string filePath, string parameter, string timestamp)
        {
            //Get folder name without path
            string folderName = "";
            string[] words = folderPath.Split('\\');
            foreach (string word in words)
            {
                folderName = word;
            }

            //Get file name without path
            string fileName = "";
            string[] words2 = filePath.Split('\\');
            foreach (string word in words2)
            {
                fileName = word;
            }

            // Copy to network drive
            string copyPath = Path.Combine(tempPath, "localCopy.bat");
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(copyPath))
            {
                file.WriteLine("@echo off");
                string dstDir = @"\\BSSFILES2\Dept\adm\labrun\temp\Custom Run\" + timestamp + @"\Custom Run\" + folderName;
                string line = @"xcopy """ + folderPath + @""" """ + dstDir + @""" /i /s /e /V /Y /Q";
                file.WriteLine(line);
            }
            service.ExecuteCommandNoOutput(copyPath, true);

            // From Network drive, to clients
            foreach (LabClient client in clients)
            {
                string batFileName = Path.Combine(tempPath, "CopyFolder" + client.ComputerName + ".bat");
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(batFileName))
                {
                    file.WriteLine("@echo off");
                    string line = @"cmdkey.exe /add:" + client.ComputerName + @" /user:" + service.Credentials.DomainSlashUser + @" /pass:" + service.Credentials.Password;
                    file.WriteLine(line);

                    // Embed xcopy command to transfer ON labclient FROM shared drive TO labclient
                    string copyCmd = @"xcopy """ + @"\\BSSFILES2\Dept\adm\labrun\temp\Custom Run\" + timestamp + @"\Custom Run\" + folderName + @"""" + @" ""C:\Cobe Lab\Custom Run\" + timestamp + @"\" + @"\Custom Run\" + folderName + @"""" + @" /i /s /e /V /Y /Q ";
                    // Build run command to embed in bat also
                    string runCmd = "";
                    // Manage parameter
                    if (parameter != "")
                        runCmd = @"""" + @"C:\Cobe Lab\Custom Run\" + timestamp + @"\" + @"\Custom Run\" + folderName + filePath + @""" """ + parameter + @"""";
                    else
                        runCmd = @"""" + @"C:\Cobe Lab\Custom Run\" + timestamp + @"\" + @"\Custom Run\" + folderName + filePath + @"""";
                    
                    // Deploy and run batfile FROM Server TO labclient using PSTools
                    line = @"C:\PSTools\PsExec.exe -d -i 1 \\" + client.ComputerName + @" -u " + service.Credentials.DomainSlashUser + @" -p " + service.Credentials.Password + @" cmd /c (" + copyCmd + @" ^& " + runCmd + @")";
                    file.WriteLine(line);
                }
                service.StartNewCmdThread(batFileName);
            }
        }

        /// <summary>
        /// Transfers a folder first to the shared drive then to each selected labclient.
        /// Uses PSExec delegated batch files, running on each client.
        /// Then runs selected file using same PSExec batch.
        /// </summary>
        /// <returns>Nothing</returns>
        public void CopyFolder(List<LabClient> clients, string folderPath, string timestamp)
        {

            //Get folder name without path
            string folderName = "";
            string[] words = folderPath.Split('\\');
            foreach (string word in words)
            {
                folderName = word;
            }      

            // Copy to network drive
            string copyPath = Path.Combine(tempPath, "localCopy.bat");
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(copyPath))
            {
                file.WriteLine("@echo off");
                string dstDir = @"\\BSSFILES2\Dept\adm\labrun\temp\Custom Run\" + timestamp + @"\Custom Run\" + folderName;
                string line = @"xcopy """ + folderPath + @""" """ + dstDir + @""" /i /s /e /V /Y /Q";
                file.WriteLine(line);
            }
            service.ExecuteCommandNoOutput(copyPath, true);

            // From Network drive, to clients
            foreach (LabClient client in clients)
            {
                string batFileName = Path.Combine(tempPath, "CopyFolder" + client.ComputerName + ".bat");
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(batFileName))
                {
                    file.WriteLine("@echo off");
                    string line = @"cmdkey.exe /add:" + client.ComputerName + @" /user:" + service.Credentials.DomainSlashUser + @" /pass:" + service.Credentials.Password;
                    file.WriteLine(line);

                    // Embed xcopy command to transfer ON labclient FROM shared drive TO labclient
                    string copyCmd = @"xcopy """ + @"\\BSSFILES2\Dept\adm\labrun\temp\Custom Run\" + timestamp + @"\Custom Run\" + folderName + @"""" + @" ""C:\Cobe Lab\Custom Run\" + timestamp + @"\" + @"\Custom Run\" + folderName + @"""" + @" /i /s /e /V /Y /Q ";

                    // Deploy and run batfile FROM Server TO labclient using PSTools
                    line = @"C:\PSTools\PsExec.exe -d -i 1 \\" + client.ComputerName + @" -u " + service.Credentials.DomainSlashUser + @" -p " + service.Credentials.Password + @" cmd /c (" + copyCmd + @")";
                    file.WriteLine(line);
                }
                service.StartNewCmdThread(batFileName);
            }
        }

        /// <summary>
        /// Deletes the temp directory on the network drive
        /// </summary>
        /// <returns>Nothing</returns> 
        public void deleteNetworkTempFiles()
        {
            string batFileName = Path.Combine(tempPath, "DeleteNetworkTemp" + ".bat");
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(batFileName))
            {
                file.WriteLine("@echo off");
                string deleteCmd = @"rmdir " + @"""\\BSSFILES2\Dept\adm\labrun\temp\""" + @" /S /Q";
                file.WriteLine(deleteCmd);
            }
            service.StartNewCmdThread(batFileName);


            string copyPath = Path.Combine("DeleteNetworkTemp" + ".bat");
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(copyPath))
            {
                file.WriteLine("@echo off");
                string line = @"rmdir /s /q """ + Path.Combine(@"\\BSSFILES2\Dept\adm\labrun\temp\Custom Run\") + @"""";
                file.WriteLine(line);
            }
            service.ExecuteCommandNoOutput(copyPath, true);
        }

        public void CloseCustomProcess(List<LabClient> computers, string exename)
        {
            Thread t = new Thread(() =>
            {
                string processName = exename;
                foreach (LabClient computer in computers)
                {
                    string cmdlet = @"Taskkill /IM " + processName + @" /F";
                    RunRemotePSCmdLet(computer.ComputerName, cmdlet);
                }
            });
            t.IsBackground = true;
            t.Start();
        }

    }
}
