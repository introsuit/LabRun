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
        private string pathToZLeaf = @"C:\shared\ZTree\zleaf.exe";

        public ZTree() : base("ZTree", @"C:\test\ZTree\ztree.exe") 
        {
            Extension = "ztt";
            ExtensionDescription = "ZTree Test Files (*.ztt)|*.ztt";

            resultExts = new string[3];
            resultExts[0] = "psydat";
            resultExts[1] = "csv";
            resultExts[2] = "log";
        }

        public override Thread TransferAndRun(List<LabClient> selectedClients)
        {
            var t = new Thread(() => xcopy(selectedClients));
            t.Start();
            return t;
        }

        private void xcopy(List<LabClient> selectedClients)
        {
            //----run ztree at admin computer
            service.ProcessStartSimple(applicationExecutableName);
            //----end

            //----run leaves with proper args
            int i = 0;
            foreach (LabClient client in selectedClients)
            {
                string batFileName = Path.Combine(tempPath, "ztreeLeaves" + client.ComputerName + ".bat");
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(batFileName))
                {
                    file.WriteLine("@echo off");

                    string srcDir = Path.Combine(service.SharedNetworkTempFolder, applicationName, testFolderName);
                    string dstDir = Path.Combine(service.TestFolder, applicationName, testFolderName);

                    string adminCompName = System.Environment.MachineName;

                    //may need adding zero in front for <10no leaves
                    string zleafNo = client.BoothNo + "";
                    if (client.BoothNo < 9)
                        zleafNo = "0" + zleafNo;

                    //resolution setting
                    string resolution = "/windowsizesmth ";

                    string runCmd = @"""" + pathToZLeaf + @""" /name Zleaf_" + zleafNo + @" /server " + adminCompName + @" /language en";
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
