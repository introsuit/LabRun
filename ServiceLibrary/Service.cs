using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.DirectoryServices;
using System.Diagnostics;
using System.Management.Automation;

namespace ServiceLibrary
{
    public class Service
    {
        private static Service service;

        public static Service getInstance()
        {
            if (service == null)
                service = new Service();
            return service;
        }

        private Service()
        {

        }

        //domain eg.: asb.local
        public static List<string> GetComputers(string domain, string username, string password)
        {
            List<string> ComputerNames = new List<string>();

            DirectoryEntry entry = new DirectoryEntry("LDAP://" + domain, username, password);
            DirectorySearcher mySearcher = new DirectorySearcher(entry);
            mySearcher.Filter = ("(objectClass=computer)");
            mySearcher.SizeLimit = int.MaxValue;
            mySearcher.PageSize = int.MaxValue;

            foreach (SearchResult resEnt in mySearcher.FindAll())
            {
                //"CN=SGSVG007DC"
                string ComputerName = resEnt.GetDirectoryEntry().Name;
                if (ComputerName.StartsWith("CN="))
                    ComputerName = ComputerName.Remove(0, "CN=".Length);
                ComputerNames.Add(ComputerName);
            }

            mySearcher.Dispose();
            entry.Dispose();

            return ComputerNames;
        }

        public void ExecuteCommand(string command, bool waitForExit = false)
        {
            //int exitCode;
            ProcessStartInfo processInfo;
            Process process;

            processInfo = new ProcessStartInfo("cmd.exe", "/c " + command);
            processInfo.CreateNoWindow = true;
            processInfo.UseShellExecute = false;
            // *** Redirect the output ***
            // processInfo.RedirectStandardError = true;
            // processInfo.RedirectStandardOutput = true;

            process = Process.Start(processInfo);
            if (waitForExit)
                process.WaitForExit();

            // *** Read the streams ***
            //string output = process.StandardOutput.ReadToEnd();
            //string error = process.StandardError.ReadToEnd();

            //exitCode = process.ExitCode;

            //MessageBox.Show("output>>" + (String.IsNullOrEmpty(output) ? "(none)" : output));
            //MessageBox.Show("error>>" + (String.IsNullOrEmpty(error) ? "(none)" : error));
            //MessageBox.Show("ExitCode: " + exitCode.ToString(), "ExecuteCommand");
            process.Close();
        }

        public void killRemoteProcess(string computerName, string processName)
        {
            Debug.WriteLine("Killing begins >:)");
            var LocalPassword = "Mandrass1";
            var ssLPassword = new System.Security.SecureString();
            foreach (char c in LocalPassword)
                ssLPassword.AppendChar(c);

            PSCredential Credential = new PSCredential("Administrator@mano", ssLPassword);
            string serverName = computerName;
            string cmdlet = "Taskkill /IM " + processName + " /F";
            using (PowerShell powershell = PowerShell.Create())
            {
                powershell.AddCommand("Set-Variable");
                powershell.AddParameter("Name", "cred");
                powershell.AddParameter("Value", Credential);

                powershell.AddScript(@"$s = New-PSSession -ComputerName '" + serverName + "' -Credential $cred");
                powershell.AddScript(@"$a = Invoke-Command -Session $s -ScriptBlock { " + cmdlet + " }");
                powershell.AddScript(@"Remove-PSSession -Session $s");
                powershell.AddScript(@"echo $a");

                var results = powershell.Invoke();

                foreach (var item in results)
                {
                    Debug.WriteLine(item);
                }

                if (powershell.Streams.Error.Count > 0)
                {
                    Debug.WriteLine("{0} errors", powershell.Streams.Error.Count);
                }
            }
        }
    }
}
