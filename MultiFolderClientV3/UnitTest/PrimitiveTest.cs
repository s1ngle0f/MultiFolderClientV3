using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Diagnostics;
using Newtonsoft.Json.Linq;


namespace MultiFolderClientV3.UnitTest
{
    public class PrimitiveTest
    {
        static MultiFolder multiFolder = new MultiFolder("test", "test");

        //public static void Main(string[] args)
        //{
        //    //GenerateSshRemote();
        //    //Ping();
        //    IsExistUser();
        //    //GetDirs();
        //    //AddDir();
        //    //DeleteDir();
        //    //AddSshKey();
        //    //Registrate();
        //    //GetListWorkingFiles();
        //    //GetWorkingFile();
        //    //GetRsaKey();
        //    //GetSessionUser();
        //    //ResetSettings();
        //    HashPassword("test");
        //    Console.ReadLine();
        //}

        public static void GenerateSshRemote()
        {
            var response = multiFolder.GenerateSshRemote("server_dir_name");
            Console.WriteLine(response);
        }

        public static void Ping()
        {
            var response = multiFolder.Ping();
            Console.WriteLine($"{response} {response.GetType()}");
        }

        public static void IsExistUser()
        {
            var response = multiFolder.IsExistUser("test", "test");
            Console.WriteLine($"{response} {response.GetType()}");
        }

        public static void GetDirs()
        {
            var response = multiFolder.GetDirs();
            Console.WriteLine($"{response.GetType()}:");
            foreach (var i in response)
            {
                Console.WriteLine($"    {i}");
            }
        }

        public static void AddDir()
        {
            var response = multiFolder.AddDir("server_dir_name");
            Console.WriteLine(response);
        }

        public static void DeleteDir()
        {
            var response = multiFolder.DeleteDir("server_dir_name");
            Console.WriteLine(response);
        }

        public static void AddSshKey()
        {
            var response = multiFolder.AddSshKey("ssh_key");
            Console.WriteLine(response);
        }

        public static void Registrate()
        {
            multiFolder.Registrate("password");
        }

        public static void GetListWorkingFiles()
        {
            var response = multiFolder.GetListWorkingFiles();
            Console.WriteLine($"{response.GetType()}:");
            foreach (var i in response)
            {
                Console.WriteLine($"    {i}");
            }
        }

        public static void GetWorkingFile()
        {
            multiFolder.GetWorkingFile("file_name", "file_path");
        }

        public static void GetRsaKey()
        {
            var rsaKey = HelpFunctions.GetRsaKey();
            Console.WriteLine(rsaKey);
        }

        public static void GetSessionUser()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var startInfo = new ProcessStartInfo
            {
                FileName = "WMIC",
                Arguments = "ComputerSystem GET UserName",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                StandardOutputEncoding = System.Text.Encoding.GetEncoding(866)
            };

            var process = Process.Start(startInfo);
            process.WaitForExit();
            string output = process.StandardOutput.ReadToEnd();
            string[] lines = output.Trim().Split('\n');
            string username = lines[1].Trim();
            Console.WriteLine(username.Split('\\')[1]);
            process.Dispose();
        }

        public static void ResetSettings()
        {
            string settingsPath = Synchronizer.settingsPath;

            if (!File.Exists(settingsPath))
            {
                using (StreamWriter sw = File.CreateText(settingsPath))
                {
                    Dictionary<string, object> data = new Dictionary<string, object>
                    {
                        { "login", "" },
                        { "password", "" },
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
                            { "login", "" },
                            { "password", "" },
                            { "directories", new List<string>() },
                            { "history", new Dictionary<string, object>() }
                        };
                        string serializedData = JsonConvert.SerializeObject(data, Formatting.Indented);
                        sw.Write(serializedData);
                    }
                }
            }
        }

        public static void SetNewLoginPassword()
        {
            string settingsPath = Synchronizer.settingsPath;

            string login = "user_test", password = "password_test";

            using (var fileStream = new FileStream(settingsPath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                using (var streamReader = new StreamReader(fileStream))
                {
                    string json = streamReader.ReadToEnd();
                    dynamic data = JsonConvert.DeserializeObject(json);

                    string lastLogin = data.login;
                    string lastPassword = data.password;

                    if (data.history[login] == null)
                    {
                        dynamic historyObject = new { directories = new string[0] };
                        data.history[login] = JObject.FromObject(historyObject);
                    }
                    if (data.history[lastLogin] == null && lastLogin != "")
                    {
                        dynamic historyObject = new { directories = new string[0] };
                        data.history[lastLogin] = JObject.FromObject(historyObject);
                    }
                    if (lastLogin != "")
                        data.history[lastLogin].directories = data.directories;
                    data.directories = data.history[login]?.directories;
                    data.login = login;
                    data.password = password;
                    json = JsonConvert.SerializeObject(data, Formatting.Indented);
                    streamReader.BaseStream.SetLength(0);
                    streamReader.BaseStream.Position = 0;
                    var writer = new StreamWriter(streamReader.BaseStream);
                    writer.Write(json);
                    writer.Flush();
                }
            }
        }

        public static void HashPassword(string password)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));

                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }

                Console.WriteLine(builder.ToString());
            }
        }
    }
}
