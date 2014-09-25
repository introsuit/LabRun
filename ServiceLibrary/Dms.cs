using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text.RegularExpressions;

namespace ServiceLibrary
{
    class Dms
    {
        private Service service = Service.getInstance();
        private string dmsUrl = "https://cobelab.au.dk";
        private MyWebClient webClient;

        public Dms()
        {
            webClient = new MyWebClient();
        }

        //returns User if logged in successful, else returns null
        public User Login(string username, string password)
        {
            User user = null;
            string userHash = webClient.DownloadString(dmsUrl + "/modules/StormDb/extract/login?username=" + username + "&password=" + password);
            if (userHash.Contains("templogin"))
            {
                user = new User(username, password);
                user.UniqueHash = userHash;
            }
            return user;
        }

        public List<string> GetProjects()
        {
            string projectsStr = webClient.DownloadString(dmsUrl + "/modules/StormDb/extract/projects?" + service.User.UniqueHash);
            string[] lines = projectsStr.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
            return new List<string>(lines);
        }

        //get all subjects from project
        private List<string> GetAllSubjects(TestApp testApp)
        {         
            string subjStr = webClient.DownloadString(dmsUrl + "/modules/StormDb/extract/subjectswithcode?" + service.User.UniqueHash + "&projectCode=" + testApp.ProjectName);
            string[] lines = subjStr.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
            return new List<string>(lines);
        }

        public void DmsTransfer(string projPath, TestApp testApp)
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
                        string subjId = CreateSubject(subject.Name, testApp);

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
        private string CreateSubject(string boothNo, TestApp testApp)
        {
            string subjId = "";
            string subjectName = "Booth" + boothNo;

            if (testApp is ZTree)
            {
                bool exists = false;
                subjectName = "ztree_subject";
                //TODO check if ztree_subject exists and if it does - use its existing subj number
                List<string> subjects = GetAllSubjects(testApp);
                foreach (string subject in subjects)
                {
                    //subject number structure: "0001_ABC"
                    if (subject.Length != 8)
                        continue;
                    string subjNoStr = subject.Remove(subject.Length - 4).TrimStart('0');

                    //ztree_subject reserved number is 34 (can be any number though)
                    if (subjNoStr == "34")
                    {
                        subjId = subject;
                        exists = true;
                        break;
                    }
                }

                if (exists)
                {
                    return subjId;
                }
            }

            //Creates subject in DMS. If successful returns new subject's id,
            //else returns a string containing "error" keyword
            string url = dmsUrl + "/modules/StormDb/extract/createsubject?subjectName=" + subjectName + "&" + service.User.UniqueHash + "&projectCode=" + testApp.ProjectName;
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

        private class MyWebClient : WebClient
        {
            protected override WebRequest GetWebRequest(Uri uri)
            {
                WebRequest w = base.GetWebRequest(uri);
                w.Timeout = 10 * 1000;
                return w;
            }
        }
    } 
}
