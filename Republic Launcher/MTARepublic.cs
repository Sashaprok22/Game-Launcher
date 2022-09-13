using System.Net;
using System.IO;
using System.Diagnostics;
using Microsoft.Win32;
using System;
using Newtonsoft.Json;

namespace Republic_Launcher
{
    public class MTARepublic
    {
        public dynamic gameServers;
        public int selectServer;

        public void updateInfo()
        {
            try
            {
                var jsonString = new StreamReader( WebRequest.Create("https://somesite.ru/servers.php").GetResponse().GetResponseStream() ).ReadToEnd();
                gameServers = JsonConvert.DeserializeObject( jsonString );
            } catch { }

        }

        public void startGame()
        {
            if (Process.GetProcessesByName("gta_sa").Length > 0 || Process.GetProcessesByName("proxy_sa").Length > 0) return;
            if ( selectServer <= 0 || selectServer > gameServers.Count) return;
            if (Convert.ToInt32(gameServers[selectServer - 1]["players"]) >= Convert.ToInt32(gameServers[selectServer - 1]["max_players"])) return;
            if (!File.Exists(Settings.GamePath + @"\MTA\Multi Theft Auto.exe")) return;

            Registry.LocalMachine.CreateSubKey(@"SOFTWARE\WOW6432Node\Multi Theft Auto: Republic");
            RegistryKey reg = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\WOW6432Node\Multi Theft Auto: Republic\Common");
            reg.SetValue("GTA:SA Path", Settings.GamePath);
            reg.Close();

            Process.Start(Settings.GamePath + @"\MTA\Multi Theft Auto.exe", "mtasa://" + gameServers[selectServer-1]["ip"] + ":" + gameServers[selectServer - 1]["port"]);
        }
    }
}
