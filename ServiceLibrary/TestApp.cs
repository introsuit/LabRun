using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;

namespace ServiceLibrary
{
    public abstract class TestApp
    {
        protected Service service = Service.getInstance();
        protected string applicationName;
        public string ApplicationName { get { return applicationName; } }
        protected string ApplicationExecutableName { get; set; }
        protected string resultsFolderName;
        protected string completionFileName = "DONE";
        protected string tempPath = Path.GetTempPath();
        public string Extension { get; set; }
        public string ExtensionDescription { get; set; }
        private string timePrint = "";
        public string testFolder { get; set; }

        protected string projectName = "";
        public string ProjectName { get { return projectName; } set { this.projectName = value; } }

        public string AppExeName
        {
            get
            {
                return Path.GetFileName(ApplicationExecutableName);
            }
        }

        protected List<string> resultExts = new List<string>();

        //e.g.: "MyTest"
        protected string testFolderName;

        //e.g.: "E:\MyTest\test1.py"
        protected string testFilePath;

        //e.g.: "test1.py"
        protected string testFileName;

        protected TestApp(string applicationName/*, string applicationExecutableName, string testFilePath*/)
        {
            this.applicationName = applicationName;
            this.testFolder = service.TestFolder;
            resultsFolderName = service.ResultsFolderName;
            //this.applicationExecutableName = applicationExecutableName;
        }

        //must be called before "Run" action!
        public void Initialize(string testFilePath)
        {
            this.testFilePath = testFilePath;
            this.testFileName = Path.GetFileName(testFilePath);
            testFolderName = Path.GetFileName(Path.GetDirectoryName(testFilePath));
        }

        //returns timestamp in yyyyMMdd_HHmmss format
        public string GetCurrentTimestamp()
        {
            return Service.getInstance().GetCurrentTimestamp();
        }

        public virtual Thread TransferAndRun(List<LabClient> selectedClients, bool copyAll)
        {
            var t = new Thread(() => xcopy(selectedClients, copyAll));
            t.Start();
            return t;
        }

        private void xcopy(List<LabClient> selectedClients, bool copyAll)
        {
            string fileArgs = "/V /Y /Q";
            string folderArgs = "/V /E /Y /Q /I";

            //-----local copy
            string copyPath = Path.Combine(tempPath, "localCopy.bat");
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(copyPath))
            {
                timePrint = GetCurrentTimestamp();

                string srcDir = testFilePath;
                string dstDir = Path.Combine(service.SharedNetworkTempFolder, applicationName, timePrint) + @"\";
                string args = fileArgs;
                if (copyAll)
                {
                    args = folderArgs;
                    srcDir = Path.GetDirectoryName(testFilePath);
                }

                file.WriteLine("@echo off");

                string line = @"xcopy """ + srcDir + @""" """ + dstDir + @""" " + args;
                file.WriteLine(line);
                if (this is PsychoPy)
                {
                    line = @"xcopy """ + Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), ((PsychoPy)this).RunWithLogsScript) + @""" """ + dstDir + @""" " + args;
                    file.WriteLine(line);
                }
            }
            service.ExecuteCommandNoOutput(copyPath, true);
            //-----end

            //---onecall to client: copy and run
            int i = 0;
            foreach (LabClient client in selectedClients)
            {
                string copyPathRemote = Path.Combine(tempPath, "remoteCopyRun" + client.ComputerName + ".bat");
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(copyPathRemote))
                {
                    file.WriteLine("@echo off");

                    string srcDir = Path.Combine(service.SharedNetworkTempFolder, applicationName, timePrint, testFileName);
                    string dstDir = Path.Combine(service.TestFolder, applicationName, timePrint, applicationName) + @"\";
                    string args = fileArgs;
                    if (copyAll)
                    {
                        args = folderArgs;
                        srcDir = Path.Combine(service.SharedNetworkTempFolder, applicationName, timePrint);
                    }
                    string copyCmd = @"xcopy """ + srcDir + @""" """ + dstDir + @""" " + args;

                    Debug.WriteLine(ApplicationExecutableName + " app exe name");
                    string runCmd = @"""" + Path.Combine(dstDir, Path.GetFileName(testFilePath)) + @"""";
                    if (this is PsychoPy)
                    {
                        //runCmd = @"""" + ApplicationExecutableName + @""" " + runCmd;
                        string runWithLogs = Path.Combine(dstDir, ((PsychoPy)this).RunWithLogsScript);
                        runCmd = ApplicationExecutableName + @" """ + runWithLogs + @""" " + @" " + runCmd;
                    }
                    string line = @"cmdkey.exe /add:" + client.ComputerName + @" /user:" + service.Credentials.DomainSlashUser + @" /pass:" + service.Credentials.Password;
                    file.WriteLine(line);

                    line = @"C:\PSTools\PsExec.exe -d -i 1 \\" + client.ComputerName + @" -u " + service.Credentials.DomainSlashUser + @" -p " + service.Credentials.Password + @" cmd /c (" + copyCmd + @" ^& cd """ + dstDir + @""" ^& start """" " + runCmd + @")";
                    file.WriteLine(line);

                }
                service.StartNewCmdThread(copyPathRemote);
                i++;
            }

            //-----notify ui
            service.notifyStatus("Request Sent");
            //-----end
        }

        private bool transferDone(List<LabClient> clients)
        {
            foreach (LabClient client in clients)
            {
                //service.SharedNetworkTempFolder + @"Results\" + applicationName + @"\"
                //string path = resultPath + "DONE" + client;
                string path = Path.Combine(service.SharedNetworkTempFolder, resultsFolderName, projectName, completionFileName + client.ComputerName);
                Debug.WriteLine(path);
                if (!File.Exists(path))
                {
                    return false;
                }
            }
            return true;
        }

        private void waitForTransferCompletion(List<LabClient> selectedClients)
        {
            long timeoutPeriod = 150000;
            int sleepTime = 5000;

            bool timedOut = false;
            Stopwatch watch = Stopwatch.StartNew();
            while (!transferDone(selectedClients) && !timedOut)
            {
                Thread.Sleep(sleepTime);
                if (watch.ElapsedMilliseconds > timeoutPeriod)
                    timedOut = true;
            }
            watch.Stop();

            if (timedOut)
                throw new TimeoutException();
        }

        public virtual Thread TransferResults(List<LabClient> clients)
        {
            var t = new Thread(() => xcopyResults(clients));
            t.IsBackground = true;
            t.Start();
            return t;
        }

        private void xcopyResults(List<LabClient> clients)
        {
            //-----clean notify files
            string copyPath = Path.Combine(tempPath, "networkClean.bat");
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(copyPath))
            {
                file.WriteLine("@echo off");
                string line = @"del /s /q " + Path.Combine(service.SharedNetworkTempFolder, resultsFolderName, projectName, completionFileName + @"*");
                file.WriteLine(line);
            }
            service.ExecuteCommandNoOutput(copyPath, true);
            //-----end

            //----copy results from client computers to shared network folder
            int i = 0;
            foreach (LabClient client in clients)
            {
                string copyPathRemote = Path.Combine(tempPath, "remoteResultOne" + client.ComputerName + ".bat");
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(copyPathRemote))
                {                  
                    file.WriteLine("@echo off");
                    string line = @"cmdkey.exe /add:" + client.ComputerName + @" /user:" + service.Credentials.DomainSlashUser + @" /pass:" + service.Credentials.Password;
                    file.WriteLine(line);

                    string src = Path.Combine(this.testFolder, applicationName);
                    string dst = Path.Combine(service.SharedNetworkTempFolder, resultsFolderName, projectName, client.BoothName + "");

                    string resultFiles = "";
                    foreach (string resultExt in resultExts)
                    {
                        resultFiles += @"xcopy """ + Path.Combine(src, "*." + resultExt) + @""" """ + dst + @""" /V /E /Y /Q /I ^& ";
                    }
                    resultFiles = resultFiles.Substring(0, resultFiles.Length - 4);

                    string completionNotifyFile = @"copy NUL " + Path.Combine(service.SharedNetworkTempFolder, resultsFolderName, projectName, completionFileName + client.ComputerName);
                    line = @"C:\PSTools\PsExec.exe -d -i 1 \\" + client.ComputerName + @" -u " + service.Credentials.DomainSlashUser + @" -p " + service.Credentials.Password + @" cmd /c (" + resultFiles + @" ^& " + completionNotifyFile + @")";
                    file.WriteLine(line);
                }
                service.StartNewCmdThread(copyPathRemote);
                i++;
            }
            //----end

            //check to make sure transfer is completed from all clients
            waitForTransferCompletion(clients);
            
            //-----copy from network to local
            copyPath = Path.Combine(tempPath, "networkResultsCopy.bat");
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(copyPath))
            {
                file.WriteLine("@echo off");
                string src = Path.Combine(service.SharedNetworkTempFolder, resultsFolderName, projectName);
                string dst = Path.Combine(service.TestFolder, resultsFolderName, projectName);
                string line = @"xcopy """ + src + @""" """ + dst + @""" /V /E /Y /Q /I" /*/Exclude:" + service.TestFolder + @"Excludes.txt"*/;
                file.WriteLine(line);
                line = @"del /s /q " + Path.Combine(dst, completionFileName + @"*");
                file.WriteLine(line);
                ////delete unneedednotif
                //line = @"del /s /q " + testFolder + @"Results\PsychoPy\DONE*";
                //file.WriteLine(line);
            }
            service.ExecuteCommandNoOutput(copyPath, true);
            //-----end

            //-----delete results from network
            copyPath = Path.Combine(tempPath, "networkResultsDelete.bat");
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(copyPath))
            {
                file.WriteLine("@echo off");
                string line = @"rmdir /s /q """ + Path.Combine(service.SharedNetworkTempFolder, resultsFolderName) + @"""";
                //string line = @"del /s /q " + Path.Combine(service.SharedNetworkTempFolder, resultsFolderName, "*.*");
                file.WriteLine(line);
            }
            service.ExecuteCommandNoOutput(copyPath, true);
            //string sharedPath = Path.Combine(service.SharedNetworkTempFolder, resultsFolderName);
            //if (Directory.Exists(sharedPath))
            //    Directory.Delete(sharedPath, true);
            //-----end

            //-----notify ui
            service.notifyStatus("Transfer Complete");
            //-----end
        }

        public virtual void DeleteResults(List<LabClient> clients)
        {
            ThreadStart ts = delegate()
            {
                //----del tests files from client computers
                int i = 0;
                foreach (LabClient client in clients)
                {
                    string copyPathRemote = Path.Combine(tempPath, "remoteTestDel" + client.ComputerName + ".bat");
                    using (System.IO.StreamWriter file = new System.IO.StreamWriter(copyPathRemote))
                    {
                        Debug.WriteLine(copyPathRemote);
                        string path = Path.Combine(service.TestFolder, applicationName);

                        string line = @"C:\PSTools\PsExec.exe -d -i 1 \\" + client.ComputerName + @" -u " + service.Credentials.DomainSlashUser + @" -p " + service.Credentials.Password + @" cmd /c (rmdir /s /q """ + path + @""")";
                        file.WriteLine(line);
                    }
                    service.StartNewCmdThread(copyPathRemote);
                    i++;
                }
                //----end

                //----del results from client computers
                i = 0;
                foreach (LabClient client in clients)
                {
                    string copyPathRemote = Path.Combine(tempPath, "remoteResultDel" + client.ComputerName + ".bat");
                    using (System.IO.StreamWriter file = new System.IO.StreamWriter(copyPathRemote))
                    {
                        Debug.WriteLine(copyPathRemote);
                        string path = Path.Combine(service.TestFolder, applicationName);

                        string line = @"C:\PSTools\PsExec.exe -d -i 1 \\" + client.ComputerName + @" -u " + service.Credentials.DomainSlashUser + @" -p " + service.Credentials.Password + @" cmd /c (rmdir /s /q """ + path + @""")";
                        file.WriteLine(line);
                    }
                    service.StartNewCmdThread(copyPathRemote);
                    i++;
                }
                //----end

                //----del results from local
                string pathDel = Path.Combine(tempPath, "delResultsFromLocal.bat");
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(pathDel))
                {
                    file.WriteLine("@echo off");
                    string line = @"rmdir /s /q """ + Path.Combine(service.TestFolder, resultsFolderName, projectName) + @"""";
                    file.WriteLine(line);
                }
                service.ExecuteCommandNoOutput(pathDel, true);
                //----end

                //-----notify ui
                service.notifyStatus("Local cleaning complete. Request sent to delete from Labclients");
                //-----end
            };
            service.RunInNewThread(ts);
        }

        public void CreateProjectDir()
        {
            string path = Path.Combine(service.TestFolder, resultsFolderName, projectName);
            Directory.CreateDirectory(path);
        }

        public virtual void OpenResultsFolder()
        {
            string path = Path.Combine(service.TestFolder, resultsFolderName, projectName);
            if (!Directory.Exists(path))
            {
                throw new DirectoryNotFoundException(path);
            }
            service.ProcessStartSimple(path);
        }

        public void ToDms()
        {
            string projPath = Path.Combine(service.TestFolder, resultsFolderName, projectName);
            if (!Directory.Exists(projPath))
            {
                throw new DirectoryNotFoundException(projPath);
            }
            service.notifyStatus("Uploading...");
            Dms dms = new Dms();
            service.RunInNewThread(() => dms.DmsTransfer(projPath, this));
        }

    }
}
