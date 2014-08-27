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
        private Service service = Service.getInstance();
        protected string applicationName;
        protected string applicationExecutableName;
        protected string resultsFolderName = "Results";
        protected string completionFileName = "DONE";
        protected string tempPath = Path.GetTempPath();
        public string Extension { get; set; }
        public string ExtensionDescription { get; set; }
        public string AppExeName 
        {
            get
            {
                return Path.GetFileName(applicationExecutableName);
            }
        }

        protected string[] resultExts;

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

        public Thread TransferAndRun(List<string> selectedClients)
        {
            var t = new Thread(() => xcopy(selectedClients));
            t.Start();
            return t;
        }

        private void xcopy(List<string> selectedClients)
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
            foreach (string computerName in selectedClients)
            {
                string copyPathRemote = Path.Combine(tempPath, "remoteCopyRun" + computerName + ".bat");
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(copyPathRemote))
                {
                    file.WriteLine("@echo off");

                    string srcDir = Path.Combine(service.SharedNetworkTempFolder, applicationName, testFolderName);
                    string dstDir = Path.Combine(service.TestFolder, applicationName, testFolderName);
                    string copyCmd = @"xcopy """ + srcDir + @""" """ + dstDir + @""" /V /E /Y /Q /I";

                    string runCmd = @"""" + applicationExecutableName + @""" """ + Path.Combine(service.TestFolder, applicationName, testFolderName, Path.GetFileName(testFilePath)) + @"""";
                    string line = @"C:\PSTools\PsExec.exe -d -i 1 \\" + computerName + @" -u " + service.DomainSlashUser + @" -p " + service.Credentials.Password + @" cmd /c (" + copyCmd + @" ^& " + runCmd + @")";

                    file.WriteLine(line);

                }
                service.StartNewCmdThread(copyPathRemote);
                i++;
            }

            //-----notify ui
            service.notifyStatus("Request Sent");
            //-----end
        }

        private bool transferDone(List<string> selectedClients)
        {
            foreach (string client in selectedClients)
            {
                //service.SharedNetworkTempFolder + @"Results\" + applicationName + @"\"
                //string path = resultPath + "DONE" + client;
                string path = Path.Combine(service.SharedNetworkTempFolder, resultsFolderName, applicationName, completionFileName + client);
                if (!File.Exists(path))
                {
                    return false;
                }
            }
            return true;
        }

        private void waitForTransferCompletion(List<string> selectedClients)
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

        public Thread TransferResults(List<string> selectedClients)
        {
            var t = new Thread(() => xcopyResults(selectedClients));
            t.Start();
            return t;
        }

        private void xcopyResults(List<string> selectedClients)
        {
            //-----clean notify files
            string copyPath = Path.Combine(tempPath, "networkClean.bat");
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(copyPath))
            {
                file.WriteLine("@echo off");
                string line = @"del /s /q " + Path.Combine(service.SharedNetworkTempFolder, resultsFolderName, applicationName, completionFileName + @"*");
                file.WriteLine(line);
            }
            service.ExecuteCommandNoOutput(copyPath, true);
            //-----end

            //----copy results from client computers to shared network folder
            int i = 0;

            foreach (string computerName in selectedClients)
            {
                string copyPathRemote = Path.Combine(tempPath, "remoteResultOne" + computerName + ".bat");
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(copyPathRemote))
                {
                    file.WriteLine("@echo off");

                    string src = Path.Combine(service.TestFolder, applicationName, testFolderName);
                    string dst = Path.Combine(service.SharedNetworkTempFolder, resultsFolderName, applicationName, computerName, testFolderName) + @""" /V /E /Y /Q /I";

                    string copyCmdpsy = @"xcopy """ + Path.Combine(src, "*." + resultExts[0]) + @""" """ + dst;
                    string copyCmdcsv = @"xcopy """ + Path.Combine(src, "*." + resultExts[1]) + @""" """ + dst;
                    string copyCmdlog = @"xcopy """ + Path.Combine(src, "*." + resultExts[2]) + @""" """ + dst;
                    
                    string completionNotifyFile = @"copy NUL " + Path.Combine(service.SharedNetworkTempFolder, resultsFolderName, applicationName, completionFileName + computerName);
                    string line = @"C:\PSTools\PsExec.exe -d -i 1 \\" + computerName + @" -u " + service.DomainSlashUser + @" -p " + service.Credentials.Password + @" cmd /c (" + copyCmdpsy + @" ^& " + copyCmdcsv + @" ^& " + copyCmdlog + @" ^& " + completionNotifyFile + @")";
                    file.WriteLine(line);
                }
                service.StartNewCmdThread(copyPathRemote);
                i++;
            }
            //----end

            //check to make sure transfer is completed from all clients
            waitForTransferCompletion(selectedClients);

            //-----copy from network to local
            copyPath = Path.Combine(tempPath, "networkResultsCopy.bat");
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(copyPath))
            {
                file.WriteLine("@echo off");
                string src = Path.Combine(service.SharedNetworkTempFolder, resultsFolderName, applicationName);
                string dst = Path.Combine(service.TestFolder, resultsFolderName, applicationName);
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
                string line = @"del /s /q " + Path.Combine(service.SharedNetworkTempFolder, resultsFolderName, applicationName, "*.*");
                file.WriteLine(line);
            }
            service.ExecuteCommandNoOutput(copyPath, true);
            //-----end

            //-----notify ui
            service.notifyStatus("Transfer Complete");
            //-----end
        }
    }
}
