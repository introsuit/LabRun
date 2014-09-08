using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace ServiceLibrary
{
    public abstract class TestApp
    {
        protected Service service = Service.getInstance();
        protected string applicationName;
        protected string applicationExecutableName;
        protected string resultsFolderName = "Results";
        protected string completionFileName = "DONE";
        protected string tempPath = Path.GetTempPath();
        public string Extension { get; set; }
        public string ExtensionDescription { get; set; }
        private string timePrint = "";

        protected string projectName = "UnknownProject";
        public string ProjectName { get { return projectName; } set { this.projectName = ProjectName; } }

        public string AppExeName
        {
            get
            {
                return Path.GetFileName(applicationExecutableName);
            }
        }

        protected List<string> resultExts = new List<string>();

        //e.g.: "MyTest"
        protected string testFolderName;

        //e.g.: "E:\MyTest\test1.py"
        protected string testFilePath;

        protected TestApp(string applicationName, string applicationExecutableName/*, string testFilePath*/)
        {
            this.applicationName = applicationName;
            this.applicationExecutableName = applicationExecutableName;
        }

        //must be called before any action!
        public void Initialize(string testFilePath)
        {
            this.testFilePath = testFilePath;
            testFolderName = Path.GetFileName(Path.GetDirectoryName(testFilePath));
        }

        public virtual Thread TransferAndRun(List<LabClient> selectedClients, string project)
        {
            projectName = project;
            var t = new Thread(() => xcopy(selectedClients));
            t.Start();
            return t;
        }

        private void xcopy(List<LabClient> selectedClients)
        {
            //-----local copy
            string copyPath = Path.Combine(tempPath, "localCopy.bat");
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(copyPath))
            {
                file.WriteLine("@echo off");
                string srcDir = Path.GetDirectoryName(testFilePath);
                string dstDir = Path.Combine(service.SharedNetworkTempFolder, applicationName, testFolderName);
                string line = @"xcopy """ + srcDir + @""" """ + dstDir + @""" /V /E /Y /Q /I";
                file.WriteLine(line);
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

                    string srcDir = Path.Combine(service.SharedNetworkTempFolder, applicationName, testFolderName);
                    string dstDir = Path.Combine(service.TestFolder, applicationName, testFolderName);
                    string copyCmd = @"xcopy """ + srcDir + @""" """ + dstDir + @""" /V /E /Y /Q /I";

                    string runCmd = @"""" + applicationExecutableName + @""" """ + Path.Combine(service.TestFolder, applicationName, testFolderName, Path.GetFileName(testFilePath)) + @"""";
                    string line = @"C:\PSTools\PsExec.exe -d -i 1 \\" + client.ComputerName + @" -u " + service.Credentials.DomainSlashUser + @" -p " + service.Credentials.Password + @" cmd /c (" + copyCmd + @" ^& start """" " + runCmd + @")";

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
            long timeoutPeriod = 120000;
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

        public virtual Thread TransferResults(List<LabClient> clients, string project)
        {
            projectName = project;
            var t = new Thread(() => xcopyResults(clients));
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
            DateTime timeStamp = DateTime.Now;
            timePrint = String.Format("{0:yyyyMMddhhmm}", timeStamp);
            int i = 0;
            foreach (LabClient client in clients)
            {
                string copyPathRemote = Path.Combine(tempPath, "remoteResultOne" + client.ComputerName + ".bat");
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(copyPathRemote))
                {
                    string src = Path.Combine(service.TestFolder, applicationName, testFolderName);

                    file.WriteLine("@echo off");
                    //file.WriteLine(@"cd " + src);
                    //file.WriteLine(@"IF NOT EXIST timestamp (echo timeis2014 > timestamp)");
                    //file.WriteLine(@"set /p time=<timestamp");
                    //file.WriteLine(@"echo %time% > timestamp");

                    string dst = Path.Combine(service.SharedNetworkTempFolder, resultsFolderName, projectName, client.BoothNo + "", timePrint, applicationName, testFolderName);

                    string resultFiles = "";
                    foreach (string resultExt in resultExts)
                    {
                        resultFiles += @"xcopy """ + Path.Combine(src, "*." + resultExt) + @""" """ + dst + @""" /V /E /Y /Q /I ^& ";
                    }
                    resultFiles = resultFiles.Substring(0, resultFiles.Length - 4);

                    string completionNotifyFile = @"copy NUL " + Path.Combine(service.SharedNetworkTempFolder, resultsFolderName, projectName, completionFileName + client.ComputerName);
                    string line = @"C:\PSTools\PsExec.exe -d -i 1 \\" + client.ComputerName + @" -u " + service.Credentials.DomainSlashUser + @" -p " + service.Credentials.Password + @" cmd /c (" + resultFiles + @" ^& " + completionNotifyFile + @")";
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
            //-----end

            //-----notify ui
            service.notifyStatus("Transfer Complete");
            //-----end
        }

        public void DeleteResults(List<LabClient> clients, string project)
        {
            projectName = project;
            new Thread(delegate()
            {
                //----del results from client computers
                int i = 0;
                foreach (LabClient client in clients)
                {
                    string copyPathRemote = Path.Combine(tempPath, "remoteResultDel" + client.ComputerName + ".bat");
                    using (System.IO.StreamWriter file = new System.IO.StreamWriter(copyPathRemote))
                    {
                        Debug.WriteLine(copyPathRemote);
                        string path = Path.Combine(service.TestFolder, applicationName, testFolderName);

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
            }).Start();
        }
    }
}
