using System;
using System.IO;
using System.Web;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Management.Automation;
using LibGit2Sharp;
using System.Diagnostics;
using Newtonsoft.Json.Linq;

namespace MultiFolderClientV3
{
    public class DirectoryRepo
    {
        public string full_path;
        public string ServerDirName;
        public bool DIRECTORY_WORK = false;
        public int time_monitring = 3;
        private MultiFolder multifolder;
        private Repository repo;

        public DirectoryRepo(string path, MultiFolder multifolder)
        {
            full_path = path;
            ServerDirName = Path.GetFileName(path);
            this.multifolder = multifolder;
            try
            {
                repo = new Repository(path);
            }
            catch (RepositoryNotFoundException)
            {
                repo = new Repository(Repository.Init(path));
            }

            try
            {
                var configName = repo.Config.Get<string>("user.name");
                if (configName.Value != multifolder.login)
                {
                    repo.Config.Set("user.name", multifolder.login);
                }
            }
            catch
            {
                repo.Config.Set("user.name", multifolder.login);
            }

            SetIgnoreTmpFiles(full_path);

            var remoteUrl = multifolder.GenerateSshRemote(ServerDirName);
            var remoteExists = false;
            foreach (var remote in repo.Network.Remotes)
            {
                if (remote.Url == remoteUrl)
                {
                    remoteExists = true;
                    break;
                }
            }

            if (!remoteExists)
            {
                if (repo.Network.Remotes != null)
                {
                    try
                    {
                        Process.Start("git", $"-C {full_path} remote remove origin");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
                Process.Start("git", $"-C {full_path} remote add origin {remoteUrl}");
            }

            multifolder.AddDir(ServerDirName);
        }

        private void SetIgnoreTmpFiles(string fullPath)
        {
            var banSymbols = new[] { "~*" };
            var excludePath = Path.Combine(fullPath, ".git", "info", "exclude");

            if (!File.Exists(excludePath))
            {
                File.Create(excludePath);
            }

            var lines = File.ReadAllLines(excludePath);
            foreach (var banSymbol in banSymbols)
            {
                if (Array.IndexOf(lines, banSymbol) >= 0)
                {
                    continue;
                }

                using (var f = new StreamWriter(excludePath, true))
                {
                    f.WriteLine(banSymbol);
                }
            }
        }

        public void Start()
        {
            DIRECTORY_WORK = true;
            var th = new Thread(monitoring);
            th.Start();
        }

        public void Stop()
        {
            DIRECTORY_WORK = false;
        }

        public void Delete()
        {
            Stop();
            Directory.Delete(full_path, true);
        }

        private void monitoring()
        {
            while (DIRECTORY_WORK && Directory.Exists(full_path))
            {
                changesDetector();
                Thread.Sleep(time_monitring * 1000);
            }
        }

        private void changesDetector()
        {
            using (PowerShell powershell = PowerShell.Create())
            {
                // this changes from the user folder that PowerShell starts up with to your git repository
                powershell.AddScript($"cd {full_path}");
                powershell.AddScript(@"git add -A");
                powershell.AddScript(@"git commit -am 'Work'");
                powershell.AddScript(@"git pull -X theirs origin master");
                powershell.AddScript(@"git add -A");
                powershell.AddScript(@"git commit -am 'Work'");
                powershell.AddScript(@"git push origin master");

                Collection<PSObject> results = powershell.Invoke();
            }
        }
    }
}
