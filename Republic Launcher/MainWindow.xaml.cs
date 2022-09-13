using System;
using System.Windows;
using System.Windows.Input;
using System.Diagnostics;
using System.Net;
using System.IO;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using System.Windows.Media;
using Ookii.Dialogs.Wpf;
using Newtonsoft.Json;
using System.Windows.Media.Imaging;

namespace Republic_Launcher
{

    public static class Settings
    {
        public static string BetaKey = "";
        public static bool haveBetaAccess = false;
        public static string GamePath = @"C:\Games\GTA Republic";
        public static int TimeCycles = 1;
        public static int GUIStyles = 1;
        public static int Images = 1;
        public static bool debug = false;
    }

    public partial class MainWindow : Window
    {

        private static Updates updMgr = new Updates();
        private static UpdateLauncher updLaunch = new UpdateLauncher();
        private static MTARepublic mtaRep = new MTARepublic();
        private List<List<UIElement>> serversUI;

        private string linkNews = "https://vk.com/respublicgroup";

        public MainWindow()
        {
            #if DEBUG
                Settings.debug = true;
            #endif
            if (Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName).Length > 1) Close();
            if (Directory.Exists("old")) Directory.Delete("old", true);

            updMgr.onUpdatePercentChange += UpdMgr_onUpdatePercentChange;
            updMgr.onUpdateComplete += UpdMgr_onUpdateComplete;

            InitializeComponent();

            if ( updLaunch.updateLauncher() )
            {
                LauncherTrouble.Visibility = Visibility.Visible;
                TroubleText.Text = "Лаунчер обновляется";
                SettingsBtn.Visibility = Visibility.Hidden;
            }
            else if (!updMgr.checkConnection())
            {
                LauncherTrouble.Visibility = Visibility.Visible;
                TroubleText.Text = "Сервера временно недоступны";
                SettingsBtn.Visibility = Visibility.Hidden;
            }
            else
            {

                try
                {
                    string vkToken = "vktoken";
                    int ownerID = 54981341;
                    var jsonString = new StreamReader(WebRequest.Create("https://api.vk.com/method/wall.get?access_token=" + vkToken + "&owner_id=-" + ownerID + "&count=5&v=5.131").GetResponse().GetResponseStream()).ReadToEnd();
                    dynamic vkResponse = JsonConvert.DeserializeObject( jsonString );
                    dynamic vkWall = vkResponse;

                    int bestTime = 0;
                    foreach( dynamic wall in vkResponse["response"]["items"] )
                    {
                        if ( (int)wall["date"].Value > bestTime )
                        {
                            vkWall = wall;
                            bestTime = (int)wall["date"].Value;
                        }
                    }

                    linkNews = "https://vk.com/club"+ ownerID + "?w=wall-" + ownerID+"_"+ vkWall["id"].Value;

                    string[] newsText = vkWall["text"].Value.Replace("\r", "").Split('\n');

                    if (newsText.Length > 1 )
                    {
                        if ( !String.IsNullOrEmpty(newsText[2]) && String.IsNullOrEmpty(newsText[1])) News.Text = newsText[0] + "\n" + newsText[2];
                        else News.Text = newsText[0] + "\n" + newsText[1];
                    } else News.Text = newsText[0];

                    DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds((int)vkWall["date"].Value).ToLocalTime();
                    NewsDate.Text = dateTime.ToString("g");

                    if (vkWall["attachments"] != null && vkWall["attachments"][0] != null && vkWall["attachments"][0]["type"].Value == "photo" && vkWall["attachments"][0]["photo"] != null)
                    {
                        dynamic photo = vkWall["attachments"][0]["photo"]["sizes"][8];

                        WebRequest.Create(photo["url"].Value).GetResponse();
                        
                        var image = new BitmapImage();
                        image.BeginInit();
                        image.UriSource = new Uri(photo["url"].Value);
                        image.DecodePixelWidth = (int)photo["width"].Value;
                        image.DecodePixelHeight = (int)photo["height"].Value;
                        image.EndInit();
                        NewsImg.Source = image;
                    }
                }
                catch { }

                if (File.Exists("settings"))
                {
                    string[] settingsInFile = File.ReadAllLines("settings");
                    if (settingsInFile.Length > 0) Settings.GamePath = settingsInFile[0];
                    if (settingsInFile.Length > 1) Settings.BetaKey = settingsInFile[1];
                    if (settingsInFile.Length > 2 && settingsInFile[2] == "0") Settings.TimeCycles = 0;
                    if (settingsInFile.Length > 3 && settingsInFile[3] == "0") Settings.GUIStyles = 0;
                    if (settingsInFile.Length > 4 && settingsInFile[4] == "0") Settings.Images = 0;
                }

                updateBetaStatus(Settings.BetaKey);

                GamePath.Text = Settings.GamePath;
                BetaEdit.Text = Settings.BetaKey;
                setCheckBoxState( TimeCycleBtn, Settings.TimeCycles );
                setCheckBoxState( GUIStylesBtn, Settings.GUIStyles );
                setCheckBoxState( ImagesBtn, Settings.Images );

                serversUI = new List<List<UIElement>> {
                    new List<UIElement>() { Server1, ServerName1, ServerPlayers1, ServerProgressBar1 },
                    new List<UIElement>() { Server2, ServerName2, ServerPlayers2, ServerProgressBar2 },
                    new List<UIElement>() { Server3, ServerName3, ServerPlayers3, ServerProgressBar3 },
                    new List<UIElement>() { Server4, ServerName4, ServerPlayers4, ServerProgressBar4 },
                    new List<UIElement>() { Server5, ServerName5, ServerPlayers5, ServerProgressBar5 },
                    new List<UIElement>() { Server6, ServerName6, ServerPlayers6, ServerProgressBar6 },
                    new List<UIElement>() { Server7, ServerName7, ServerPlayers7, ServerProgressBar7 },
                    new List<UIElement>() { Server8, ServerName8, ServerPlayers8, ServerProgressBar8 },
                    new List<UIElement>() { Server9, ServerName9, ServerPlayers9, ServerProgressBar9 },
                    new List<UIElement>() { Server10, ServerName10, ServerPlayers10, ServerProgressBar10 },
                };
                tickUpdateServers(this, EventArgs.Empty);
                var timer = new DispatcherTimer();
                timer.Tick += new EventHandler(tickUpdateServers);
                timer.Interval = new TimeSpan(0, 0, 15);
                timer.Start();

                tickCheckGTASA(this, EventArgs.Empty);
                timer = new DispatcherTimer();
                timer.Tick += new EventHandler(tickCheckGTASA);
                timer.Interval = new TimeSpan(0, 0, 5);
                timer.Start();

            }

            HeaderBackground.MouseDown += delegate (object Sender, MouseButtonEventArgs e) { if (e.ChangedButton == MouseButton.Left) this.DragMove(); };
        }

        private void updateBetaStatus(string key)
        {
            Settings.BetaKey = key;
            if (key.Length != 16) {
                Settings.haveBetaAccess = false;
                return;
            }

            try
            {
                var response = new StreamReader(WebRequest.Create(Updates.updatesUrl + "beta_access.php?key=" + key).GetResponse().GetResponseStream()).ReadToEnd();
                Settings.haveBetaAccess = Int32.Parse(response) == 1;
            }
            catch { Settings.haveBetaAccess = false; }
        }

        private void onSettingsClick( object sender, RoutedEventArgs e )
        {
            if ( sender == CloseSettingsBtn ) SettingsWindow.Visibility = Visibility.Hidden;
            else if ( sender == SelectGamePath )
            {
                if (updMgr.getUpdateStatus() != 0) return;
                var dialog = new VistaFolderBrowserDialog();
                dialog.Description = "Выберете каталог где будет находиться GTA Republic";
                dialog.UseDescriptionForTitle = true;
                bool result = (bool)dialog.ShowDialog();
                if (result)
                {
                    Settings.GamePath = dialog.SelectedPath;
                    GamePath.Text = dialog.SelectedPath;
                    saveSettings();
                }
            }
            else if (sender == ActivateBetaKey)
            {
                updateBetaStatus(BetaEdit.Text);
                tickUpdateServers(this, EventArgs.Empty);
                saveSettings();
            }
            else if ( sender == TimeCycleBtn )
            {
                if (Settings.TimeCycles == 1) Settings.TimeCycles = 0; else Settings.TimeCycles = 1;
                setCheckBoxState( TimeCycleBtn, Settings.TimeCycles );
                saveSettings();
            }
            else if (sender == GUIStylesBtn)
            {
                if (Settings.GUIStyles == 1) Settings.GUIStyles = 0; else Settings.GUIStyles = 1;
                setCheckBoxState(GUIStylesBtn, Settings.GUIStyles);
                saveSettings();
            }
            else if (sender == ImagesBtn)
            {
                if (Settings.Images == 1) Settings.Images = 0; else Settings.Images = 1;
                setCheckBoxState(ImagesBtn, Settings.Images);
                saveSettings();
            }

        }

        private void onServerClick( object sender, RoutedEventArgs e )
        {
            for (int i = 0; i < 10; i++)
            {
                if ( sender == serversUI[i][0] )
                {
                    if ( mtaRep.selectServer != 0 )
                    {
                        ((Button)serversUI[mtaRep.selectServer - 1][0]).IsEnabled = true;
                        ((Button)serversUI[mtaRep.selectServer - 1][0]).BorderBrush = (SolidColorBrush)(new BrushConverter().ConvertFrom("#252836"));
                    }
                    ((Button)serversUI[i][0]).IsEnabled = false;
                    ((Button)serversUI[i][0]).BorderBrush = (SolidColorBrush)(new BrushConverter().ConvertFrom("#467FF9"));
                    if (mtaRep.selectServer == 0)
                    {
                        StartGame.Content = "Начать играть";
                        StartGame.IsEnabled = true;
                    }
                    mtaRep.selectServer = i+1;
                    return;
                }
                
            }
        }

        private void onMainClick(object sender, RoutedEventArgs e)
        {
            if (sender == CloseBtn) Close();
            else if (sender == MinimizedBtn) WindowState = WindowState.Minimized;
            else if (sender == SettingsBtn)
            {
                BetaEdit.Text = Settings.BetaKey;
                SettingsWindow.Visibility = Visibility.Visible;
            }
            else if (sender == SiteBtn) Process.Start("https://gtarepublic.ru/donate");
            else if (sender == DiscordBtn) Process.Start("https://discord.com/invite/Yxw5G2SWTx");
            else if (sender == VKBtn) Process.Start("https://vk.com/respublicgroup");
            else if (sender == ReadNews) Process.Start(linkNews);
            else if (sender == StartGame)
            {
                int updateStatus = updMgr.getUpdateStatus();
                if (updateStatus == 2)
                {
                    updMgr.cancelUpdate();
                    Percents.Visibility = Visibility.Hidden;
                    ProgressBar.Visibility = Visibility.Hidden;
                    FileDownload.Visibility = Visibility.Hidden;
                    SpeedConnection.Text = "0";
                    StartGame.Content = "Начать играть";
                    TaskBar.ProgressState = System.Windows.Shell.TaskbarItemProgressState.None;
                    TaskBar.ProgressValue = 0;
                }
                else if (updateStatus == 0)
                {
                    if (mtaRep.selectServer < 1 || mtaRep.selectServer > mtaRep.gameServers.Count) return;
                    if (!updMgr.updateGame()) return;

                    StartGame.Content = "Отменить";
                    Percents.Text = "Проверка файлов";
                    Percents.Visibility = Visibility.Visible;

                    ProgressBar.Width = 300;
                    ProgressBar.Visibility = Visibility.Visible;

                    FileDownload.Text = "Подождите несколько секунд";
                    FileDownload.Visibility = Visibility.Visible;
                    SpeedConnection.Text = "0";

                    TaskBar.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
                }

            }

        }

        private void UpdMgr_onUpdateComplete(object sender, EventArgs e)
        {
            Percents.Visibility = Visibility.Hidden;
            ProgressBar.Visibility = Visibility.Hidden;
            FileDownload.Visibility = Visibility.Hidden;
            SpeedConnection.Text = "0";
            StartGame.Content = "Начать играть";
            TaskBar.ProgressState = System.Windows.Shell.TaskbarItemProgressState.None;
            TaskBar.ProgressValue = 0;
            mtaRep.startGame();
            tickCheckGTASA(this, EventArgs.Empty);
        }

        private void UpdMgr_onUpdatePercentChange(object sender, updatePercentChangeArgs e)
        {
            Percents.Text = Math.Round( e.percents ).ToString() + "%";
            TaskBar.ProgressValue = (double)(e.percents/100m);
            var nWidth = (double)(542m / 100m * e.percents);
            ProgressBar.Width = nWidth >= 10 ? nWidth : ( e.percents < 1 ? 0 : 10 );
            FileDownload.Text = "Файл - " + e.file;
            if (e.speed > 0) SpeedConnection.Text = e.speed.ToString();
        }

        private void tickUpdateServers( object sender, EventArgs e )
        {
            mtaRep.updateInfo();
            updateBetaStatus(Settings.BetaKey);
            int sharedPlayersCount = 0;
                
            for ( int i = 0; i < 10; i++ )
            {
                Button btn = (Button)serversUI[i][0];
                TextBlock name = (TextBlock)serversUI[i][1];
                TextBlock players = (TextBlock)serversUI[i][2];
                Rectangle progressBar = (Rectangle)serversUI[i][3];

                if ( i + 1 > mtaRep.gameServers.Count || (mtaRep.gameServers[i]["dev"] == true && !Settings.haveBetaAccess ) )
                {
                    BlurEffect blur = new BlurEffect();
                    blur.Radius = 5;
                    blur.KernelType = KernelType.Gaussian;
                    btn.Effect = blur;
                    btn.IsEnabled = false;
                    if (mtaRep.selectServer == i+1) btn.BorderBrush = (SolidColorBrush)(new BrushConverter().ConvertFrom("#252836"));
                }
                else
                {
                    name.Text = mtaRep.gameServers[i]["name"];
                    players.Text = mtaRep.gameServers[i]["players"] + "/" + mtaRep.gameServers[i]["max_players"];

                    progressBar.Width = 150d / 100d * ((double)Convert.ToInt32(mtaRep.gameServers[i]["players"]) / (double)Convert.ToInt32(mtaRep.gameServers[i]["max_players"]) * 100d);
                    BlurEffect blur = new BlurEffect();
                    blur.Radius = 0;
                    blur.KernelType = KernelType.Gaussian;
                    btn.Effect = blur;

                    btn.IsEnabled = true;

                    sharedPlayersCount += Convert.ToInt32(mtaRep.gameServers[i]["players"]);
                }
            }

            AllPlayersCount.Text = sharedPlayersCount.ToString();
        }
        
        private void tickCheckGTASA( object sender, EventArgs e )
        {
            if (updMgr.getUpdateStatus() != 0) return;
            if (Process.GetProcessesByName("gta_sa").Length > 0 || Process.GetProcessesByName("proxy_sa").Length > 0)
            {
                StartGame.Content = "Запущено";
                StartGame.IsEnabled = false;
            }
            else if (mtaRep.selectServer == 0)
            {
                StartGame.Content = "Выберите сервер";
                StartGame.IsEnabled = false;
            }
            else
            {
                StartGame.Content = "Начать играть";
                StartGame.IsEnabled = true;
            }
        }

        private void saveSettings()
        {
            File.WriteAllText("settings", Settings.GamePath + "\n" + Settings.BetaKey + "\n" + Settings.TimeCycles + "\n" + Settings.GUIStyles + "\n" + Settings.Images);
        }

        private void setCheckBoxState( Button checkBox, int state )
        {
            if (state == 1)
            {
                checkBox.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#467FF9"));
                checkBox.BorderBrush = (SolidColorBrush)(new BrushConverter().ConvertFrom("#467FF9"));
            }
            else
            {
                checkBox.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#252836"));
                checkBox.BorderBrush = (SolidColorBrush)(new BrushConverter().ConvertFrom("#2F3142"));
            }
        }
    }
}
