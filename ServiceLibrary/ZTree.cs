using System.Collections.Generic;
using System.Threading;
using System.IO;

namespace ServiceLibrary
{
    /// <summary>
    /// Relies on correct ztree.vbs launcher settings
    /// </summary>
    public class ZTree : TestApp
    {
        private readonly string ztreeAdminExe;
        private readonly string dumpFolder;

        public ZTree()
            : base("ztree")
        {
            ApplicationExecutableName = service.Config.Ztreeleaf;
            ztreeAdminExe = service.Config.Ztreeadmin;
            dumpFolder = service.Config.Ztreedump;

            resultExts.Add("xls");
            resultExts.Add("sbj");
            resultExts.Add("pay");
            resultExts.Add("pay");
        }

        public void RunAdminZTree()
        {
            new Thread(() =>
            {
                string resultsFolder = Path.Combine(dumpFolder, GetCurrentTimestamp(), applicationName);
                if (!Directory.Exists(resultsFolder))
                {
                    Directory.CreateDirectory(resultsFolder);
                }
                //string path = @"C:\ZTree\ztree.exe";
                //string arguments = @"/language en /privdir " + dumpFolder + @" /datadir " + dumpFolder + @" /gsfdir " + dumpFolder;
                //service.LaunchCommandLineApp(path, arguments);

                //service.LaunchCommandLineApp(@"C:\ZTree\JustRun.vbs", "");
                //Process.Start(@"C:\ZTree\JustRun.vbs");

                string copyPath = Path.Combine(tempPath, "ztreeRun.bat");
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(copyPath))
                {
                    file.WriteLine("@echo off");
                    string line = "cd " + Path.GetDirectoryName(ztreeAdminExe);
                    file.WriteLine(line);
                  
                    line = @"start """" " + Path.GetFileName(ztreeAdminExe) + @" /language en /privdir " + resultsFolder + @" /datadir " + resultsFolder + @" /gsfdir " + resultsFolder;
                    file.WriteLine(line);
                }
                service.StartNewCmdThread(copyPath);
            }).Start();
        }

        public Thread TransferAndRun(List<LabClient> selectedClients, WindowSize windowSize)
        {
            var t = new Thread(() => xcopy(selectedClients, windowSize));
            t.Start();
            return t;
        }

        private void xcopy(List<LabClient> selectedClients, WindowSize windowSize)
        {
            //----run leaves with proper args
            int i = 0;
            foreach (LabClient client in selectedClients)
            {
                string batFileName = Path.Combine(tempPath, "ztreeLeaves" + client.ComputerName + ".bat");
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(batFileName))
                {
                    file.WriteLine("@echo off");
                    string adminCompName = System.Environment.MachineName;

                    //adds zero in front for <10no leaves
                    string zleafNo = client.BoothNo + "";
                    if (client.BoothNo < 9)
                        zleafNo = "0" + zleafNo;

                    //window setting
                    string windSize = "/size " + windowSize.Width + "x" + windowSize.Height;
                    string windPos = "/position " + windowSize.XPos + "," + windowSize.YPos;

                    string line = @"cmdkey.exe /add:" + client.ComputerName + @" /user:" + service.Credentials.DomainSlashUser + @" /pass:" + service.Credentials.Password;
                    file.WriteLine(line);

                    string runCmd = @"""" + ApplicationExecutableName + @""" /name Zleaf_" + zleafNo + @" /server " + adminCompName + @" /language en " + windSize + " " + windPos;
                    line = @"C:\PSTools\PsExec.exe -d -i 1 \\" + client.ComputerName + @" -u " + service.Credentials.DomainSlashUser + @" -p " + service.Credentials.Password + @" " + runCmd;
                    file.WriteLine(line);
                }
                service.StartNewCmdThread(batFileName);
                i++;
            }
            //----end

            //-----notify ui
            service.notifyStatus("Request Sent");
            //-----end
        }

        public override Thread TransferResults(List<LabClient> clients)
        {
            var t = new Thread(() => ResTransfer(clients));
            t.IsBackground = true;
            t.Start();
            return t;
        }

        private void ResTransfer(List<LabClient> clients)
        {
            //-----copy from ztree dir to local res dir
            string copyPath = Path.Combine(tempPath, "resCopyLocal.bat");
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(copyPath))
            {
                file.WriteLine("@echo off");
                string src = dumpFolder;
                string dst = Path.Combine(service.TestFolder, resultsFolderName, projectName, "ZTreeSubject");
                string line = @"xcopy """ + src + @""" """ + dst + @""" /V /E /Y /Q /I";
                file.WriteLine(line);
                //line = @"del /s /q " + Path.Combine(dst, completionFileName + @"*");
                //file.WriteLine(line);
            }
            service.ExecuteCommandNoOutput(copyPath, true);
            //-----end

            //-----notify ui
            service.notifyStatus("Transfer Complete");
            //-----end
        }

        public override void DeleteResults(List<LabClient> clients)
        {
            new Thread(delegate()
            {
                //----del results from local
                //kill ztree.exe first to avoid "files are being using" error
                service.KillLocalProcess("ztree.exe");

                string pathDel = Path.Combine(tempPath, "delResultsFromLocal.bat");
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(pathDel))
                {
                    file.WriteLine("@echo off");
                    string line = @"rmdir /s /q """ + Path.Combine(dumpFolder) + @"""";
                    file.WriteLine(line);
                    line = @"rmdir /s /q """ + Path.Combine(service.TestFolder, resultsFolderName, projectName) + @"""";
                    file.WriteLine(line);
                }
                service.ExecuteCommandNoOutput(pathDel, true);
                //----end

                //-----notify ui
                service.notifyStatus("Cleaning Complete");
                //-----end
            }).Start();
        }
    }
}
