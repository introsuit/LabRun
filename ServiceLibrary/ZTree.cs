using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Diagnostics;

namespace ServiceLibrary
{
    /// <summary>
    /// Relies on correct ztree.vbs launcher settings
    /// </summary>
    public class ZTree : TestApp
    {
        private readonly string pathToZTreeAdmin = @"C:\ZTree\ZTreeRun.vbs";
        private readonly string dumpFolder = @"C:\ZTreeDump";

        public ZTree()
            : base("ZTree", @"C:\Cobe Lab\ZTree\ZTree\zleaf.exe")
        {
            applicationName = "ZTree";

            resultExts.Add("xls");
            resultExts.Add("sbj");
            resultExts.Add("pay");
            resultExts.Add("pay");
        }

        public void RunAdminZTree()
        {
            new Thread(() =>
            {
                if (!Directory.Exists(dumpFolder))
                {
                    Directory.CreateDirectory(dumpFolder);
                }
                string path = @"\\asb.local\staff\users\labclient\ZTree\ZTree\ztree.exe";
                string arguments = @"/language en /privdir " + dumpFolder + @" /datadir " + dumpFolder + @" /gsfdir " + dumpFolder;
                service.LaunchCommandLineApp(path, arguments);
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

                    //may need adding zero in front for <10no leaves
                    string zleafNo = client.BoothNo + "";
                    if (client.BoothNo < 9)
                        zleafNo = "0" + zleafNo;

                    //window setting
                    string windSize = "/size " + windowSize.Width + "x" + windowSize.Height;
                    string windPos = "/position " + windowSize.XPos + "," + windowSize.YPos;

                    string runCmd = @"""" + applicationExecutableName + @""" /name Zleaf_" + zleafNo + @" /server " + adminCompName + @" /language en " + windSize + " " + windPos;
                    string line = @"C:\PSTools\PsExec.exe -d -i 1 \\" + client.ComputerName + @" -u " + service.Credentials.DomainSlashUser + @" -p " + service.Credentials.Password + @" " + runCmd;
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
                string dst = Path.Combine(service.TestFolder, resultsFolderName, projectName, "ZTreeSubject", applicationName);
                string line = @"xcopy """ + src + @""" """ + dst + @""" /V /E /Y /Q /I";
                file.WriteLine(line);
                //line = @"del /s /q " + Path.Combine(dst, completionFileName + @"*");
                //file.WriteLine(line);
            }
            service.ExecuteCommandNoOutput(copyPath, true);
            //-----end
        }

        public override void DeleteResults(List<LabClient> clients)
        {
            new Thread(delegate()
            {
                //----del results from local
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
            }).Start();
        }
    }
}
