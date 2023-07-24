using System;
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
        private readonly float _timeSleep = 1;
        public string Version { get; private set; }
        public Updater() {}

        public Updater(float timeSleep)
        {
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
                GetVersion();
                
                Thread.Sleep((int) (_timeSleep*1000));
            }
        }

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
