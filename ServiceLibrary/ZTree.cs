using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Diagnostics;

namespace ServiceLibrary
{
    public class ZTree : TestApp
    {
        private string pathToZTreeAdmin = @"C:\ZTree\ztree.exe";

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

                    //resolution setting
                    string winSize = "/size " + windowSize.Width + "x" + windowSize.Height;

                    string runCmd = @"""" + applicationExecutableName + @""" /name Zleaf_" + zleafNo + @" /server " + adminCompName + @" /language en " + winSize;
                    string line = @"C:\PSTools\PsExec.exe -d -i 1 \\" + client.ComputerName + @" -u " + service.DomainSlashUser + @" -p " + service.Credentials.Password + @" " + runCmd;
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
    }
}
