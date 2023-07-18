using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Diagnostics;
using Newtonsoft.Json.Linq;

namespace MultiFolderClientV3.UnitTest
{
    public class AppTest
    {
        private static string settingsPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/MultiFolder/settings.json";
        //public static void Main(string[] args)
        //{
        //    //CheckGitVersion();
        //    //ReadSettingJson();
        //    //MonitoringSettingsJson();
        //    //DeleteFromSettingsIfNotExist();

        //    Synchronizer app = new Synchronizer();
        //    app.Start();
        //}

        public static void CheckGitVersion()
        {
            var process = Process.Start("git", "--version");
            Console.WriteLine($"{process} ---");
        }

        public static void ReadSettingJson()
        {
            using (StreamReader sr = File.OpenText(settingsPath))
            {
                Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(sr.ReadToEnd());
                Console.WriteLine(data);
                foreach (KeyValuePair<string, object> item in data)
                {
                    Console.WriteLine($"{item.Key} {item.Value} {item.Value.GetType()}");
                }
            }
        }

        public static void MonitoringSettingsJson()
        {
            JToken lastData = null;
            int timeUpdate = 3;
            for (int i = 0; i < 10; i++)
            {
                var data = JToken.Parse(File.ReadAllText(settingsPath));
                if (lastData != null)
                {
                    var result = HelpFunctions.CompareLists(lastData["directories"].ToObject<List<string>>(), data["directories"].ToObject<List<string>>());
                    foreach (var path in result["add"])
                    {
                        Console.WriteLine($"Add: {path}");
                    }
                    foreach (var path in result["remove"])
                    {
                        Console.WriteLine($"Remove: {path}");
                    }
                }
                lastData = data;
                Thread.Sleep(timeUpdate * 1000);
            }
        }

        private static void DeleteFromSettingsIfNotExist()
        {
            var data = JToken.Parse(File.ReadAllText(settingsPath));
            var dirs = data["directories"].ToObject<List<string>>();
            dirs.RemoveAll(dir => !Directory.Exists(dir));
            data["directories"] = JToken.FromObject(dirs);
            File.WriteAllText(settingsPath, data.ToString());
        }
    }
}
