using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using LibGit2Sharp;
using System.Diagnostics;
using System.Management.Automation;

namespace MultiFolderClientV3.UnitTest
{
    public class DirectoryRepoTest
    {
        public static string path = "C:\\Users\\zubko\\Desktop\\DATA\\ЗубковМБП";
        public static Repository repo;
        public static MultiFolder multifolder = new MultiFolder("user1", "user1");

        //public static void Main(string[] args)
        //{
        //    //GetUserName();
        //    CommitAndPush();
        //}

        public static void GetUserName()
        {
            try
            {
                repo = new Repository(path);
            }
            catch (RepositoryNotFoundException)
            {
                repo = new Repository(Repository.Init(path));
            }

            var configName = repo.Config.Get<string>("user.name");
            Console.WriteLine(configName.Value);
        }

        public static void CommitAndPush()
        {
            using (PowerShell powershell = PowerShell.Create())
            {
                // this changes from the user folder that PowerShell starts up with to your git repository
                powershell.AddScript($"cd {path}");
                powershell.AddScript(@"git add -A");
                powershell.AddScript(@"git commit -m 'WorkTest'");
                powershell.AddScript(@"git push origin master");

                Collection<PSObject> results = powershell.Invoke();
            }
        }
    }
}
