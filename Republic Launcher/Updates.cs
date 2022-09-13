using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Security.Cryptography;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Linq;

namespace Republic_Launcher
{
    public class updatePercentChangeArgs : EventArgs
    {
        public float speed = 0;
        public decimal percents = 0;
        public string file = "";
        public bool afterCheck = false;
    }

    public class Updates
    {
        public const string updatesUrl = "https://somesite.ru/";

        public string downloadingFile;
        public List<float> downloadingSpeed = new List<float> { 0 };
        public decimal downloadPercent;

        public event EventHandler<updatePercentChangeArgs> onUpdatePercentChange;
        public event EventHandler onUpdateComplete;

        private WebClient webClient = new WebClient();
        private string[] gameFiles = { };
        private List<string> filesToUpdate = new List<string> { };

        private int fileIndex = 0;
        private DateTime lastSpeedUpdateTime;
        private long lastSpeedUpdateBytes = 0;

        private bool isUpdatesChecking = false;

        public Updates()
        {
            webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(onFileDownloadProgressChange);
            webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(onFileDownloadComplete);
        }

        private void afterCheckUpdates(bool haveUpdates)
        {
            webClient.CancelAsync();
            isUpdatesChecking = false;

            if (haveUpdates)
            {
                fileIndex = 0;
                downloadingFile = filesToUpdate[fileIndex];
                downloadPercent = 0;
                onUpdatePercentChange( this, new updatePercentChangeArgs() );

                downloadFile(new Uri(updatesUrl + "\\GameFiles\\" + downloadingFile), Settings.GamePath + "\\" + downloadingFile);
            }
            else
            {
                downloadingFile = null;
                downloadPercent = 0;
                fileIndex = 0;
                filesToUpdate = new List<string> { };
                onUpdateComplete(this, EventArgs.Empty);
            }
        }

        public async void checkUpdates()
        {
            isUpdatesChecking = true;
            if (String.IsNullOrEmpty(Settings.GamePath) || gameFiles.Length < 1)
            {
                afterCheckUpdates(false);
                return;
            }

            filesToUpdate = new List<string> { };
            var comprasionTasks = new Task<string>[gameFiles.Length];
            for (int index = 0; index < gameFiles.Length; index++)
            {
                string strFileInfo = gameFiles[index];
                comprasionTasks[index] = Task.Run(() =>
                {
                    string[] fileInfo = strFileInfo.Split(':');
                    if (fileInfo.Length < 2) return "";

                    string fileName = fileInfo[1];
                    string fileHash = fileInfo[0];

                    bool isExists = File.Exists(Settings.GamePath + "\\" + fileName);

                    if (isExists)
                    {
                        if (Settings.TimeCycles != 1 && fileName == "data\\timecyc.dat") return "";
                        if (Settings.GUIStyles != 1 && fileName.Contains("MTA\\skins")) return "";
                        if (Settings.Images != 1 && fileName.Contains("MTA\\mta\\cgui\\images")) return "";
                    }

                    if (!isExists || getFileHash(Settings.GamePath + "\\" + fileName) != fileHash) return fileName;
                    return "";
                });

            }
            await Task.Delay(1);
            Task.WaitAll(comprasionTasks);
            foreach ( var task in comprasionTasks)
            {
                if ( !String.IsNullOrEmpty( task.Result ) ) filesToUpdate.Add( task.Result );
            }

            afterCheckUpdates(filesToUpdate.Count > 0);
        }

        public bool checkConnection()
        {
            try
            {
                gameFiles = webClient.DownloadString(updatesUrl + "info/gameFiles").Replace("\r", "").Split('\n');
                return true;
            }
            catch
            {
                gameFiles = new string[] { };
                return false;
            }
        }

        public bool updateGame()
        {
            if (String.IsNullOrEmpty(Settings.GamePath)) return false;
            if (!checkConnection()) return false;
            webClient.CancelAsync();
            checkUpdates();
            return true;
        }

        public int getUpdateStatus() { return webClient.IsBusy ? 2 : isUpdatesChecking ? 1 : 0; }

        public void cancelUpdate()
        {
            webClient.CancelAsync();

            lastSpeedUpdateBytes = 0;
            lastSpeedUpdateTime = DateTime.Now;
            downloadingFile = null;
            downloadPercent = 0;
            fileIndex = 0;
        }

        private void onFileDownloadProgressChange(object Sender, DownloadProgressChangedEventArgs e)
        {
            decimal percentFor1File = 100m / filesToUpdate.Count;
            downloadPercent = percentFor1File * fileIndex + percentFor1File / 100m * e.ProgressPercentage;

            if (lastSpeedUpdateBytes == 0)
            {
                lastSpeedUpdateTime = DateTime.Now;
                lastSpeedUpdateBytes = e.BytesReceived;
            }
            else if ((DateTime.Now - lastSpeedUpdateTime).Milliseconds > 50)
            {
                var now = DateTime.Now;
                var timeSpan = now - lastSpeedUpdateTime;
                var bytesChange = e.BytesReceived - lastSpeedUpdateBytes;
                downloadingSpeed.Add((float)Math.Round(bytesChange / timeSpan.Milliseconds * 10000m / 1024m / 1024m) / 10f);
                if (downloadingSpeed.Count > 5) downloadingSpeed.RemoveAt(0);

                lastSpeedUpdateBytes = e.BytesReceived;
                lastSpeedUpdateTime = now;
            }



            updatePercentChangeArgs args = new updatePercentChangeArgs();
            args.speed = Enumerable.Average(downloadingSpeed);
            args.percents = downloadPercent;
            args.file = filesToUpdate[fileIndex];
            onUpdatePercentChange(this, args);
        }

        private void onFileDownloadComplete(object Sender, AsyncCompletedEventArgs e)
        {
            if (e.Cancelled) return;
            fileIndex++;
            if (fileIndex + 1 >= filesToUpdate.Count)
            {
                downloadingFile = null;
                downloadPercent = 0;
                fileIndex = 0;
                filesToUpdate = new List<string> { };

                onUpdateComplete(this, EventArgs.Empty);
            }
            else
            {
                downloadingFile = filesToUpdate[fileIndex];
                downloadPercent = 100 / filesToUpdate.Count * fileIndex;
                downloadFile(new Uri(updatesUrl + "\\GameFiles\\" + downloadingFile), Settings.GamePath + "\\" + downloadingFile);
            }
        }

        private void downloadFile(Uri uri, string fileName)
        {
            lastSpeedUpdateBytes = 0;
            lastSpeedUpdateTime = DateTime.Now;
            FileInfo fileInfo = new FileInfo(fileName);
            if (!Directory.Exists(fileInfo.Directory.FullName)) Directory.CreateDirectory(fileInfo.Directory.FullName);
            webClient.DownloadFileAsync(uri, fileName);
        }

        public static string getFileHash(string filePath)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    byte[] checksum = md5.ComputeHash(stream);
                    return BitConverter.ToString(checksum).Replace("-", String.Empty).ToUpper();
                }
            }

        }
    }
}
