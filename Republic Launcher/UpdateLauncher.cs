using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;

namespace Republic_Launcher
{

    public class UpdateLauncher
    {
        public const string updatesUrl = "https://somesite.ru/";

        private WebClient webClient = new WebClient();
        private string[] gameFiles = { };
        private List<string> filesToUpdate = new List<string> { };

        private int fileIndex = 0;

        public UpdateLauncher()
        {
            webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(onFileDownloadComplete);
        }

        public bool checkUpdates()
        {
            if (Settings.debug) return false;
            try
            {
                string serverRequest = webClient.DownloadString(updatesUrl + "info/launcherFiles").Replace("\r", "");
                gameFiles = serverRequest.Split('\n');
            }
            catch { return false; }

            if (gameFiles.Length < 1) return false;

            filesToUpdate = new List<string> { };
            foreach (string strFileInfo in gameFiles)
            {
                string[] fileInfo = strFileInfo.Split(':');
                if (fileInfo.Length < 2) continue;

                string fileName = fileInfo[1];
                string fileHash = fileInfo[0];

                if (!File.Exists(fileName) || Updates.getFileHash(fileName) != fileHash) filesToUpdate.Add(fileName);
            }
            return (filesToUpdate.Count > 0);
        }

        public bool updateLauncher()
        {
            if (!checkUpdates()) return false;

            webClient.CancelAsync();
            fileIndex = 0;

            downloadFile(new Uri(updatesUrl + "\\LauncherFiles\\" + filesToUpdate[fileIndex]), filesToUpdate[fileIndex]);
            return true;
        }

        private void onFileDownloadComplete(object Sender, AsyncCompletedEventArgs e)
        {
            if (e.Cancelled) return;
            fileIndex++;
            if (fileIndex+1 >= filesToUpdate.Count)
            {
                if (File.Exists( "temp\\Republic Launcher.exe"))
                {
                    Directory.CreateDirectory("old");
                    File.Move(Process.GetCurrentProcess().MainModule.ModuleName, "old\\"+ Process.GetCurrentProcess().MainModule.ModuleName);
                    File.Move("temp\\Republic Launcher.exe", "Republic Launcher.exe");
                    Directory.Delete("temp");
                }
                Process.Start("Republic Launcher.exe");
                Application.Current.Shutdown();
            }
            else
            {
                downloadFile(new Uri(updatesUrl + "\\LauncherFiles\\" + filesToUpdate[fileIndex]), filesToUpdate[fileIndex]);
            }
        }

        private void downloadFile(Uri uri, string fileName)
        {
            if (fileName == "Republic Launcher.exe") fileName = "temp\\" + fileName;
            FileInfo fileInfo = new FileInfo(fileName);
            if (!Directory.Exists(fileInfo.Directory.FullName)) Directory.CreateDirectory(fileInfo.Directory.FullName);
            webClient.DownloadFileAsync(uri, fileName);
        }
    }
}


