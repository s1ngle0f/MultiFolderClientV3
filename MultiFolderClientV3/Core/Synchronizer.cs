using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Reflection;
using Newtonsoft.Json.Linq;


namespace MultiFolderClientV3
{
    public class Synchronizer
    {
        public static string Version { get; private set; } = Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
        public static string ExeName { get; private set; } = AppDomain.CurrentDomain.FriendlyName;
        public static string ExePath { get; private set; } = HelpFunctions.GetExePath();
        public static string StartupFolderPath { get; private set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "Startup");
        // public static string ExePath { get; private set; } = HelpFunctions.GetExePath(@"C:\Users\zubko\source\repos\MultiFolderClientV3\MultiFolderClientV3\bin\Debug\MultiFolderClientV3.exe"); //Для тестов
        // public static string settingsPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/MultiFolder/settings.json"; //Для итоговой версии
        public static string settingsPath = Path.Combine(Path.GetDirectoryName(ExePath), "settings.json"); //Для работы напрямую, где компилируется проект
        public static string settingsDirPath = Path.GetDirectoryName(settingsPath);
        private string login;
        private string userToken;
        public List<DirectoryRepo> directories = new List<DirectoryRepo>();
        private bool isWork = true;
        public MultiFolder multifolder;

        public Synchronizer(bool isTest = false)
        {
            if (isTest)
            {
                ExePath = HelpFunctions.GetExePath(
                    @"C:\Users\zubko\source\repos\MultiFolderClientV3\MultiFolderClientV3\bin\Debug\MultiFolderClientV3.exe");
                settingsPath = Path.Combine(Path.GetDirectoryName(ExePath), "settings.json");
                settingsDirPath = Path.GetDirectoryName(settingsPath);
            }

            var shortcutPath = Path.Combine(StartupFolderPath, "MultiFolderV3.lnk");
            if (!File.Exists(shortcutPath))
                HelpFunctions.CreateShortcut(shortcutPath, ExePath);

            // СДЕЛАТЬ ПРОВЕРКУ НА ГИТ И ПРИ ЕГО ОСТУТСТВИИ УСТАНОВИТЬ!
            try
            {
                if (Process.Start("git", "--version") == null)
                {
                    Process.Start(Path.GetDirectoryName(settingsPath) + "/git_installer.exe");
                }
            }
            catch (Exception e)
            {
                Process.Start(Path.GetDirectoryName(settingsPath) + "/git_installer.exe");
            }

            resetSettings();

            using (StreamReader sr = File.OpenText(settingsPath))
            {
                Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(sr.ReadToEnd());
                login = (string)data["login"];
                userToken = (string)data["userToken"];
                multifolder = new MultiFolder(login, userToken);
            }

            DeleteFromSettingsIfNotExist();
        }

        ~Synchronizer()
        {
            Stop();
        }

        public void Start()
        {
            if (!string.IsNullOrEmpty(login) && !string.IsNullOrEmpty(userToken) &&
                multifolder.IsExistUser(login, userToken))
            {
                using (StreamReader sr = File.OpenText(settingsPath))
                {
                    Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(sr.ReadToEnd());
                    List<string> dirPaths = JsonConvert.DeserializeObject<List<string>>(data["directories"].ToString());
                    foreach (string dirPath in dirPaths)
                    {
                        ObserveDirectory(dirPath);
                    }
                }
                multifolder.AddSshKey(HelpFunctions.GetRsaKey());
                Thread monitoring = new Thread(new ThreadStart(StartMonitoring));
                monitoring.Start();
            }
        }

        public void StopDir(string dirName)
        {
            foreach (DirectoryRepo dir in directories)
            {
                if (dir.ServerDirName == dirName)
                {
                    dir.Stop();
                }
            }
        }

        public void DeleteDir(string dirName)
        {
            foreach (DirectoryRepo dir in directories)
            {
                if (dir.ServerDirName == dirName)
                {
                    dir.Delete();
                }
            }
        }

        public void Stop()
        {
            foreach (DirectoryRepo dir in directories)
            {
                dir.Stop();
            }
            StopMonitoring();
        }

        private void StartMonitoring()
        {
            JToken lastData = null;
            int timeUpdate = 3;
            while (isWork)
            {
                var data = JToken.Parse(File.ReadAllText(settingsPath));
                if (lastData != null)
                {
                    var result = HelpFunctions.CompareLists(lastData["directories"].ToObject<List<string>>(), data["directories"].ToObject<List<string>>());
                    foreach (var path in result["add"])
                    {
                        AddObservingDirectory(path);
                    }
                    foreach (var path in result["remove"])
                    {
                        StopObservingDirectory(path);
                    }
                }
                DeleteFromSettingsIfNotExist();
                lastData = data;
                Thread.Sleep(timeUpdate * 1000);
            }
        }

        private void StopMonitoring()
        {
            isWork = false;
        }

        private void ObserveDirectory(string path)
        {
            var directory = new DirectoryRepo(path, multifolder);
            directories.Add(directory);
            var serverDirs = multifolder.GetDirs();
            if (!serverDirs.Contains(Path.GetFileName(path)))
            {
                multifolder.AddDir(Path.GetFileName(path));
            }
            directory.Start();
        }

        private void AddObservingDirectory(string path)
        {
            multifolder.AddDir(Path.GetFileName(path));
            ObserveDirectory(path);
        }

        private void DeleteObservingDirectory(string path)
        {
            foreach (var dir in directories)
            {
                if (dir.ServerDirName == Path.GetFileName(path))
                {
                    multifolder.DeleteDir(dir.ServerDirName);
                    dir.Delete();
                }
            }
        }

        private void StopObservingDirectory(string path)
        {
            foreach (var dir in directories)
            {
                if (dir.ServerDirName == Path.GetFileName(path))
                {
                    dir.Stop();
                }
            }
        }

        private void DeleteFromSettingsIfNotExist()
        {
            var data = JToken.Parse(File.ReadAllText(settingsPath));
            var dirs = data["directories"].ToObject<List<string>>();
            dirs.RemoveAll(dir => !Directory.Exists(dir));
            data["directories"] = JToken.FromObject(dirs);
            HelpFunctions.WaitFreedomFile(settingsPath);
            File.WriteAllText(settingsPath, data.ToString());
        }

        private void resetSettings()
        {
            if (!File.Exists(settingsPath))
            {
                using (StreamWriter sw = File.CreateText(settingsPath))
                {
                    Dictionary<string, object> data = new Dictionary<string, object>
                    {
                        { "version", "" },
                        { "login", "" },
                        { "userToken", "" },
                        { "directories", new List<string>() },
                        { "history", new Dictionary<string, object>() }
                    };
                    string serializedData = JsonConvert.SerializeObject(data, Formatting.Indented);
                    sw.Write(serializedData);
                }
            }
            else
            {
                Dictionary<string, object> _data;

                using (StreamReader sr = File.OpenText(settingsPath))
                {
                    _data = JsonConvert.DeserializeObject<Dictionary<string, object>>(sr.ReadToEnd());
                }

                if (_data == null)
                {
                    using (StreamWriter sw = File.CreateText(settingsPath))
                    {
                        Dictionary<string, object> data = new Dictionary<string, object>
                        {
                            { "version", "" },
                            { "login", "" },
                            { "userToken", "" },
                            { "directories", new List<string>() },
                            { "history", new Dictionary<string, object>() }
                        };
                        string serializedData = JsonConvert.SerializeObject(data, Formatting.Indented);
                        sw.Write(serializedData);
                    }
                }
            }
        }
    }
}
