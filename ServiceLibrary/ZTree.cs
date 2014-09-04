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
        private string pathToZTreeAdmin = @"C:\ZTree\ZTreeRun.vbs";

        public ZTree()
            : base("ZTree", @"C:\Cobe Lab\ZTree\ZTree\zleaf.exe")
        {
            //Extension = "ztt";
            //ExtensionDescription = "ZTree Test Files (*.ztt)|*.ztt";

            resultExts.Add("xls");
            resultExts.Add("sbj");
            resultExts.Add("pay");
            resultExts.Add("pay");
        }

        public Thread TransferAndRun(List<LabClient> selectedClients, WindowSize windowSize)
        {
            var t = new Thread(() => xcopy(selectedClients, windowSize));
            t.Start();
            return t;
        }

        private void xcopy(List<LabClient> selectedClients, WindowSize windowSize)
        {
            //----run ztree at admin computer
            service.ProcessStartSimple(pathToZTreeAdmin);
            //----end

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
                string src = Path.Combine(Path.GetDirectoryName(pathToZTreeAdmin), "Results");
                string dst = Path.Combine(service.TestFolder, resultsFolderName, projectName, "ZTreeSubject");
                string line = @"xcopy """ + src + @""" """ + dst + @""" /V /E /Y /Q /I";
                file.WriteLine(line);
                //line = @"del /s /q " + Path.Combine(dst, completionFileName + @"*");
                //file.WriteLine(line);
            }
            service.ExecuteCommandNoOutput(copyPath, true);
            //-----end
        }
    }
}
