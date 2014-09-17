using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace ServiceLibrary
{
    public abstract class TestApp
    {
        protected Service service = Service.getInstance();
        protected string applicationName;
        protected string ApplicationExecutableName { get; set; }
        protected string resultsFolderName = "Results";
        protected string completionFileName = "DONE";
        protected string tempPath = Path.GetTempPath();
        public string Extension { get; set; }
        public string ExtensionDescription { get; set; }
        private string timePrint = "";

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
            //this.applicationExecutableName = applicationExecutableName;
        }

        //must be called before any action!
        public void Initialize(string testFilePath)
        {
            this.testFilePath = testFilePath;
            this.testFileName = Path.GetFileName(testFilePath);
            testFolderName = Path.GetFileName(Path.GetDirectoryName(testFilePath));
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
                string srcDir = testFilePath;
                string dstDir = Path.Combine(service.SharedNetworkTempFolder, applicationName, testFolderName) + @"\";
                string args = fileArgs;
                if (copyAll)
                {
                    args = folderArgs;
                    srcDir = Path.GetDirectoryName(testFilePath);
                }

                file.WriteLine("@echo off");

                string line = @"xcopy """ + srcDir + @""" """ + dstDir + @""" " + args;
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

                    string srcDir = Path.Combine(service.SharedNetworkTempFolder, applicationName, testFolderName, testFileName);
                    string dstDir = Path.Combine(service.TestFolder, applicationName, testFolderName) + @"\";
                    string args = fileArgs;
                    if (copyAll)
                    {
                        args = folderArgs;
                        srcDir = Path.Combine(service.SharedNetworkTempFolder, applicationName, testFolderName);
                    }
                    string copyCmd = @"xcopy """ + srcDir + @""" """ + dstDir + @""" " + args;

                    Debug.WriteLine(ApplicationExecutableName + " app exe name");
                    string runCmd = @"""" + Path.Combine(service.TestFolder, applicationName, testFolderName, Path.GetFileName(testFilePath)) + @"""";
                    if (this is PsychoPy)
                    {
                        runCmd = @"""" + ApplicationExecutableName + @""" " + runCmd;
                    }
                    Debug.WriteLine(runCmd + " arun cmd");
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
            DateTime timeStamp = DateTime.Now;
            timePrint = String.Format("{0:yyyyMMdd_hhmmss}", timeStamp);
            int i = 0;
            foreach (LabClient client in clients)
            {
                string copyPathRemote = Path.Combine(tempPath, "remoteResultOne" + client.ComputerName + ".bat");
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(copyPathRemote))
                {
                    string src = Path.Combine(service.TestFolder, applicationName);

                    file.WriteLine("@echo off");
                    //file.WriteLine(@"cd " + src);
                    //file.WriteLine(@"IF NOT EXIST timestamp (echo timeis2014 > timestamp)");
                    //file.WriteLine(@"set /p time=<timestamp");
                    //file.WriteLine(@"echo %time% > timestamp");

                    string dst = Path.Combine(service.SharedNetworkTempFolder, resultsFolderName, projectName, client.BoothNo + "", timePrint, applicationName);

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

        public virtual void DeleteResults(List<LabClient> clients)
        {
            new Thread(delegate()
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

                //-----notify ui
                service.notifyStatus("Cleaning Complete");
                //-----end
            }).Start();
        }

        public virtual void OpenResultsFolder()
        {
            service.ProcessStartSimple(Path.Combine(service.TestFolder, resultsFolderName));
        }

        public void ToDms()
        {
            if (service.User == null)
                throw new Exception("You must login first!");

            string projPath = Path.Combine(service.TestFolder, resultsFolderName, projectName);
            DirectoryInfo[] subjects = new DirectoryInfo(projPath).GetDirectories();

            //get all subjects from project
            WebClient webClient = new WebClient();
            string subjStr = webClient.DownloadString("https://cobelab.au.dk/modules/StormDb/extract/subjectswithcode?" + service.User.UniqueHash + "&projectCode=" + projectName);
            string[] lines = subjStr.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
            List<string> dmsSubjects = new List<string>(lines);

            List<string> zipsForUpload = new List<string>();

            //iterate through all subjects and their results
            foreach (DirectoryInfo subject in subjects)
            {
                foreach (DirectoryInfo timeline in subject.GetDirectories())
                {
                    foreach (DirectoryInfo modality in timeline.GetDirectories())
                    {
                        if (modality.Name != applicationName)
                        {
                            continue;
                        }
                        int boothId = Convert.ToInt32(subject.Name);
                        string subjId = GetSubjId(dmsSubjects, boothId);

                        string dirToZip = Path.Combine(projPath, subject.Name, timeline.Name, applicationName);
                        string zipFileName = ProjectName + "." + subjId + "." + timeline.Name + "." + applicationName + ".zip";                      
                        string zipPath = Path.Combine(projPath, zipFileName);

                        ZipDirectory(dirToZip, zipPath);
                        zipsForUpload.Add(zipPath);
                    }
                }
            }

            //finally upload all the zips to network drive
            UploadZips(zipsForUpload);
        }

        private string GetSubjId(List<string> subjects, int boothNo)
        {
            string subjId = "";

            //check if subj exists
            bool exists = false;
            foreach (string subject in subjects)
            {
                //subject number structure: "0001_ABC"
                if (subject.Length != 8)
                    continue;
                string subjNoStr = subject.Remove(subject.Length - 4).TrimStart('0');
                int subjNo = Convert.ToInt32(subjNoStr);
                if (subjNo == boothNo)
                {
                    subjId = subject;
                    exists = true;
                    break;
                }
            }

            //if exists - retrieve id, else create new subj and get its id
            if (!exists)
            {
                WebClient webClient = new WebClient();
                string url = "https://cobelab.au.dk/modules/StormDb/extract/createsubject?subjectNo=" + boothNo + "&subjectName=Booth" + boothNo + "&" + service.User.UniqueHash + "&projectCode=" + projectName;
                string result = webClient.DownloadString(url);
                //cut "\n" - new line seperators from result
                result = Regex.Replace(result, @"\n", String.Empty);
                subjId = result;
                if (subjId.Contains("error"))
                    throw new Exception("Failed to create new subject for BoothNo " + boothNo);
            }
            return subjId;
        }

        private void ZipDirectory(string dirToZip, string zipPath)
        {
            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }
            ZipFile.CreateFromDirectory(@dirToZip, @zipPath);
        }

        private void UploadZips(List<string> zips)
        {
            string uploadPath = @service.Config.DmsUpload;
            string copyPath = tempPath + "zipsUpload.bat";
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(copyPath))
            {
                file.WriteLine("@echo off");
                file.WriteLine(@"net use """ + uploadPath + @""" " + service.User.Password + @" /user:" + service.User.Username);

                //file.WriteLine(":copy");
                foreach (string zip in zips)
                {
                    string line = @"xcopy """ + zip + @""" """ + uploadPath + @""" /V /Y /Q /I";
                    file.WriteLine(line);
                }
                
                //file.WriteLine("IF ERRORLEVEL 0 goto disconnect");
                //file.WriteLine("goto disconnect");

                //file.WriteLine(":disconnect");
                file.WriteLine(@"net use """ + uploadPath + @""" /delete");
                //file.WriteLine("goto end");
                //file.WriteLine(":end");
                file.WriteLine("exit");
            }
            service.ExecuteCommandNoOutput(copyPath, true);

            if (File.Exists(copyPath))
                File.Delete(copyPath);
        }
    }
}
