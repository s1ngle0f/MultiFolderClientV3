﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MultiFolderClientV3.Core
{
    public class Updater
    {
        private readonly string SUFFIX = "y8FfZm_Temp";
        private readonly float _timeSleep = 1;
        public string Version { get; private set; }
        private Synchronizer synchronizer;

        public Updater(Synchronizer synchronizer, float timeSleep)
        {
            this.synchronizer = synchronizer;
            _timeSleep = timeSleep;
        }

        public void Start()
        {
            Thread monitoring = new Thread(new ThreadStart(StartMonitoring));
            monitoring.Start();
        }

        private void StartMonitoring()
        {
            while (true)
            {
                var VersionFromServer = synchronizer.multifolder.GetVersionFromServer();
                if(Version != VersionFromServer)
                    Console.WriteLine(123);
                Thread.Sleep((int) (_timeSleep*1000));
            }
        }

        private void Update()
        {
            //Get-ChildItem -Path "C:\путь_к_папке"
            //Get-ChildItem -Filter "*123---321*" -Recurse | ForEach-Object { Rename-Item $_.FullName $_.Name.Replace("123---321", "") }
            //del file.txt
            Dictionary<string, string> serverFilesWithHash = synchronizer.multifolder.GetListWorkingFiles(WithHash: true);
            List<string> filesForUpdate = GetFilesByDifferentHash(serverFilesWithHash);
            List<string> filesForDelete = GetFilesForDelete(serverFilesWithHash, filesForUpdate);
            DownloadNewFiles(filesForUpdate);
            string commandForDelete = GetCommandDeleteOldFiles(filesForDelete);
            HelpFunctions.Cmd($"taskkill /f /im \"{Synchronizer.ExeName}\" && timeout /t 7 && {commandForDelete} && " +
                // "Get-ChildItem -Filter \"*" + SUFFIX + "*\" -Recurse | ForEach-Object { Rename-Item $_.FullName $_.Name.Replace(\"" + SUFFIX + "\", \"\") }");
                "Get-ChildItem -Path \"" + Synchronizer.settingsDirPath + "\" -Filter \"*" + SUFFIX + "*\" -Recurse | ForEach-Object { Rename-Item $_.FullName $_.Name.Replace(\"" + SUFFIX + "\", \"\") } && " +
                $"\"{Synchronizer.ExePath}\"", true);
        }

        private void DownloadNewFiles(List<string> filesRelativePath)
        {
            var basePath = Path.GetDirectoryName(Synchronizer.settingsPath);
            foreach (string filePath in filesRelativePath)
            {
                synchronizer.multifolder.GetWorkingFile(filePath, AddSuffixToEnd(Path.Combine(basePath, filePath), SUFFIX));
            }
        }

        private string AddSuffixToEnd(string filePath, string suffix)
        {
            string directory = Path.GetDirectoryName(filePath);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
            string extension = Path.GetExtension(filePath);

            string newFileName = $"{fileNameWithoutExtension}{suffix}{extension}";
            string newPath = Path.Combine(directory, newFileName);

            return newPath;
        }

        private string GetCommandDeleteOldFiles(List<string> filesRelativePath)
        {
            string res = "";
            foreach (string fileRelPath in filesRelativePath)
            {
                if(fileRelPath != filesRelativePath.LastOrDefault())
                    res += $"del \"{Path.Combine(Path.GetDirectoryName(Synchronizer.settingsPath), fileRelPath)}\" && ";
                else
                    res += $"del \"{Path.Combine(Path.GetDirectoryName(Synchronizer.settingsPath), fileRelPath)}\"";
            }

            // string lastSymbol = " && ";
            // int lastIndex = res.LastIndexOf(lastSymbol);
            // if (lastIndex != -1)
            // {
            //     // Удаляем последнюю комбинацию " && "
            //     res = res.Remove(lastIndex, lastSymbol.Length);
            // }
            return res;
        }

        private List<string> GetFilesForDelete(Dictionary<string, string> serverFilesWithHash, List<string> filesByDiffHash)
        {
            List<string> unusedFiles = HelpFunctions.CompareLists(serverFilesWithHash.Keys.ToList(), HelpFunctions.GetAllFilesDirectory(Path.GetDirectoryName(Synchronizer.settingsPath)))["add"];
            foreach (string unusedFile in unusedFiles)
            {
                if(!filesByDiffHash.Contains(unusedFile))
                {
                    filesByDiffHash.Add(unusedFile);
                }
            }
            if(filesByDiffHash.Contains("settings.json"))
                filesByDiffHash.Remove("settings.json");

            return filesByDiffHash;
        }

        private List<string> GetFilesByDifferentHash(Dictionary<string, string> filesHash)
        {
            List<string> res = new List<string>();
            foreach (string filesRelativePath in filesHash.Keys)
            {
                var basePath = Path.GetDirectoryName(Synchronizer.settingsPath);
                var localPath = Path.Combine(basePath, filesRelativePath);
                if (File.Exists(localPath))
                {
                    var localHash = HelpFunctions.HashFile(localPath);
                    var serverHash = filesHash[filesRelativePath];
                    if(localHash != serverHash)
                        res.Add(filesRelativePath);
                }
                else
                    res.Add(filesRelativePath);
            }
            return res;
        }

        [Obsolete("Не имеет смысла, так как в Synchronizer имеется Version")]
        private string GetVersion()
        {
            using (StreamReader sr = File.OpenText(Synchronizer.settingsPath))
            {
                Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(sr.ReadToEnd());
                Version = (string) data["version"];
            }
            return Version;
        }

        private string GetVersionFromServer()
        {
            using (StreamReader sr = File.OpenText(Synchronizer.settingsPath))
            {
                Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(sr.ReadToEnd());
                Version = (string)data["version"];
            }
            return Version;
        }
    }
}
