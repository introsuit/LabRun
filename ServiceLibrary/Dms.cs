using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ServiceLibrary
{
    class Dms
    {
        private Service service = Service.getInstance();
        private TestApp testApp;

        public Dms(TestApp testApp){
            this.testApp = testApp;
        }

        public void DmsTransfer(string projPath)
        {
            DirectoryInfo[] subjects = new DirectoryInfo(projPath).GetDirectories();
            List<string> zipsForUpload = new List<string>();

            //iterate through all subjects and their results
            foreach (DirectoryInfo subject in subjects)
            {
                foreach (DirectoryInfo timeline in subject.GetDirectories())
                {
                    foreach (DirectoryInfo modality in timeline.GetDirectories())
                    {
                        if (modality.Name != testApp.ApplicationName)
                        {
                            continue;
                        }
                        string subjId = CreateSubject(subject.Name);

                        string dirToZip = Path.Combine(projPath, subject.Name, timeline.Name, testApp.ApplicationName);
                        string zipFileName = testApp.ProjectName + "." + subjId + "." + timeline.Name + "." + testApp.ApplicationName + ".zip";
                        string zipPath = Path.Combine(projPath, zipFileName);

                        ZipDirectory(dirToZip, zipPath);
                        zipsForUpload.Add(zipPath);
                    }
                }
            }

            //finally upload all the zips to network drive
            UploadZips(zipsForUpload);
        }

         //creates subject and returns subject number
        private string CreateSubject(string boothNo, List<string> subjects = null)
        {
            string subjId = "";
            string subjectName = "Booth" + boothNo;

            if (testApp is ZTree)
            {
                subjectName = "ztree_subject";
                //TODO check if ztree_subject exists and if it does - use its existing subj number
                
            }

            WebClient webClient = new WebClient();
            string url = "https://cobelab.au.dk/modules/StormDb/extract/createsubject?subjectName=" + subjectName + "&" + service.User.UniqueHash + "&projectCode=" + testApp.ProjectName;
            string result = webClient.DownloadString(url);
            //cut "\n" - new line seperators from result
            result = Regex.Replace(result, @"\n", String.Empty);
            subjId = result;
            if (subjId.Contains("error"))
                throw new Exception("Failed to create new subject for BoothNo " + boothNo);
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
            string copyPath = Path.GetTempPath() + "zipsUpload.bat";
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(copyPath))
            {
                file.WriteLine("@echo off");
                file.WriteLine(@"net use """ + uploadPath + @""" " + service.User.Password + @" /user:" + service.User.Username);

                file.WriteLine(":copy");
                foreach (string zip in zips)
                {
                    string line = @"xcopy """ + zip + @""" """ + uploadPath + @""" /V /Y /Q /I";
                    file.WriteLine(line);
                }

                file.WriteLine("IF ERRORLEVEL 0 goto disconnect");
                file.WriteLine("goto disconnect");

                file.WriteLine(":disconnect");
                file.WriteLine(@"net use """ + uploadPath + @""" /delete");
                file.WriteLine("goto end");
                file.WriteLine(":end");
                file.WriteLine("exit");
            }
            service.ExecuteCommandNoOutput(copyPath, true);

            if (File.Exists(copyPath))
                File.Delete(copyPath);
        }
    }
}
