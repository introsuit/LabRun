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

        protected TestApp()
        {

        }

        public Thread TransferAndRun(string srcDir, string dstDir, string testExePath, List<string> selectedClients)
        {
            var t = new Thread(() => xcopy(srcDir, dstDir, testExePath, selectedClients));
            t.Start();
            return t;
        }

        private void xcopy(string srcDir, string dstDir, string testExePath, List<string> selectedClients)
        {
            //-----local copy
            string copyPath = service.TempPath + "localCopy.bat";
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(copyPath))
            {
                file.WriteLine("@echo off");
                string line = @"xcopy """ + srcDir + @""" " + @"""" + service.SharedNetworkTempFolder + applicationName + @"\" + dstDir + @""" /V /E /Y /Q /I";
                file.WriteLine(line);
            }
            //MessageBox.Show(copyPath);
            service.ExecuteCommandNoOutput(copyPath, true);
            //-----end

            //---onecall
            int i = 0;
            foreach (string computerName in selectedClients)
            {
                string copyPathRemote = service.TempPath + "remoteCopyOne" + computerName + ".bat";
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(copyPathRemote))
                {
                    file.WriteLine("@echo off");

                    string copyCmd = @"xcopy """ + service.SharedNetworkTempFolder + applicationName + @"\" + dstDir + @""" """ + service.TestFolder + applicationName + @"\" + dstDir + @""" /V /E /Y /Q /I";
                    string runCmd = applicationExecutableName + @" " + service.TestFolder + applicationName + @"\" + dstDir + @"\" + testExePath;
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


        private bool transferDone(List<string> selectedClients, string resultPath)
        {
            foreach (string client in selectedClients)
            {
                string path = resultPath + "DONE" + client;
                if (!File.Exists(path))
                {
                    return false;
                }
            }
            return true;
        }

        private void waitForTransferCompletion(List<string> selectedClients, string resultPath)
        {
            long timeoutPeriod = 120000;
            int sleepTime = 5000;

            bool timedOut = false;
            Stopwatch watch = Stopwatch.StartNew();
            while (!transferDone(selectedClients, resultPath) && !timedOut)
            {
                Thread.Sleep(sleepTime);
                if (watch.ElapsedMilliseconds > timeoutPeriod)
                    timedOut = true;
            }
            watch.Stop();

            if (timedOut)
                throw new TimeoutException();
        }

        public Thread TransferResults(string srcWithoutComputerName, string dstFolderName, List<string> selectedClients)
        {
            var t = new Thread(() => xcopyResults(srcWithoutComputerName, dstFolderName, selectedClients));
            t.Start();
            return t;
        }

        private void xcopyResults(string srcWithoutComputerName, string dstFolderName, List<string> selectedClients)
        {
            //-----clean notify files
            string copyPath = service.TempPath + "networkClean.bat";
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(copyPath))
            {
                file.WriteLine("@echo off");
                string line = @"del /s /q " + service.SharedNetworkTempFolder + @"Results\" + applicationName + @"\DONE*";
                file.WriteLine(line);
            }
            service.ExecuteCommandNoOutput(copyPath, true);
            //-----end

            //----copy results from client computers to shared network folder
            int i = 0;
            foreach (string computerName in selectedClients)
            {
                string copyPathRemote = service.TempPath + "remoteResultOne" + computerName + ".bat";
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(copyPathRemote))
                {
                    file.WriteLine("@echo off");
                    string copySrc = service.TestFolder + applicationName + @"\" + dstFolderName + @"\";
                    string copyDst = service.SharedNetworkTempFolder + @"Results\" + applicationName + @"\" + computerName + @"\" + dstFolderName + @""" /V /E /Y /Q /I";
                    string copyCmdpsy = @"xcopy """ + copySrc + @"*.psydat""" + @" """ + copyDst;
                    string copyCmdcsv = @"xcopy """ + copySrc + @"*.csv""" + @" """ + copyDst;
                    string copyCmdlog = @"xcopy """ + copySrc + @"*.log""" + @" """ + copyDst;
                    string completionNotifyFile = @"copy NUL " + service.SharedNetworkTempFolder + @"Results\" + applicationName + @"\DONE" + computerName;
                    string line = @"C:\PSTools\PsExec.exe -d -i 1 \\" + computerName + @" -u " + service.DomainSlashUser + @" -p " + service.Credentials.Password + @" cmd /c (" + copyCmdpsy + @" ^& " + copyCmdcsv + @" ^& " + copyCmdlog + @" ^& " + completionNotifyFile + @")";
                    file.WriteLine(line);
                }
                service.StartNewCmdThread(copyPathRemote);
                i++;
            }
            //----end

            //check to make sure transfer is completed from all clients
            waitForTransferCompletion(selectedClients, service.SharedNetworkTempFolder + @"Results\" + applicationName + @"\");

            //-----copy from network to local
            copyPath = service.TempPath + "networkResultsCopy.bat";
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(copyPath))
            {
                file.WriteLine("@echo off");
                string line = @"xcopy """ + service.SharedNetworkTempFolder + @"Results\" + applicationName + @""" """ + service.TestFolder + @"Results"" /V /E /Y /Q /I /Exclude:" + service.TestFolder + @"Excludes.txt";
                file.WriteLine(line);
                ////delete unneenotif
                //line = @"del /s /q " + testFolder + @"Results\PsychoPy\DONE*";
                //file.WriteLine(line);
            }
            service.ExecuteCommandNoOutput(copyPath, true);
            //-----end

            //-----delete results from network
            copyPath = service.TempPath + "networkResultsDelete.bat";
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(copyPath))
            {
                file.WriteLine("@echo off");
                string line = @"del /s /q " + service.SharedNetworkTempFolder + @"Results\" + applicationName + @"\*.*";
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
