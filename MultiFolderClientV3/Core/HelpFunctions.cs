using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Security.Cryptography;
using System.Diagnostics;
using Newtonsoft.Json;

namespace MultiFolderClientV3
{
    public static class HelpFunctions
    {
        public static string HashPassword(string password)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));

                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }

                return builder.ToString();
            }
        }

        public static void WaitFreedomFile(string filePath)
        {
            while (true)
            {
                try
                {
                    using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                    {
                        // Файл свободен
                        break;
                    }
                }
                catch (IOException)
                {

                }
                Thread.Sleep(100);
            }
        }

        public static string GetRsaKey()
        {
            string homeUserSsh = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ssh");
            if (!Directory.Exists(homeUserSsh))
            {
                Directory.CreateDirectory(homeUserSsh);
            }

            string idRsa = Path.Combine(homeUserSsh, "id_rsa");
            string idRsaPub = Path.Combine(homeUserSsh, "id_rsa.pub");

            if (!File.Exists(idRsa) || !File.Exists(idRsaPub))
            {
                File.WriteAllText(idRsaPub, string.Empty);
                using (Process process = new Process())
                {
                    process.StartInfo.FileName = "ssh-keygen";
                    process.StartInfo.Arguments = $"-t rsa -b 4096 -f \"{idRsa}\" -P \"\"";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.Start();

                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        throw new Exception($"ssh-keygen process failed with code {process.ExitCode}. Error output: {error}");
                    }
                }
            }

            string rsaKey = File.ReadLines(idRsaPub).FirstOrDefault();
            return rsaKey;
        }

        public static List<List<T>> Chunked<T>(List<T> data, int num)
        {
            List<List<T>> res = new List<List<T>>();
            int newNum = data.Count / num;

            for (int i = 0; i < data.Count; i += newNum)
            {
                res.Add(data.GetRange(i, Math.Min(newNum, data.Count - i)));
            }

            if (res.Count > num)
            {
                res[res.Count - 2].AddRange(res.Last());
                res.RemoveAt(res.Count - 1);
            }

            return res;
        }

        public static long GetSize(string startPath = ".")
        {
            long totalSize = 0;
            foreach (string file in Directory.EnumerateFiles(startPath, "*", SearchOption.AllDirectories))
            {
                FileInfo fileInfo = new FileInfo(file);
                if (!fileInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
                {
                    totalSize += fileInfo.Length;
                }
            }

            return totalSize;
        }

        public static Dictionary<string, List<string>> CompareLists(List<string> first, List<string> second)
        {
            Dictionary<string, List<string>> result = new Dictionary<string, List<string>>()
            {
                { "add", new List<string>() },
                { "remove", new List<string>() }
            };

            foreach (string item in first)
            {
                if (!second.Contains(item))
                {
                    result["remove"].Add(item);
                }
            }

            foreach (string item in second)
            {
                if (!first.Contains(item))
                {
                    result["add"].Add(item);
                }
            }

            return result;
        }

        public static T Clone<T>(T source)
        {
            // Don't serialize a null object, simply return the default for that object
            if (ReferenceEquals(source, null)) return default;

            // initialize inner objects individually
            // for example in default constructor some list property initialized with some values,
            // but in 'source' these items are cleaned -
            // without ObjectCreationHandling.Replace default constructor values will be added to result
            var deserializeSettings = new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace };

            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(source), deserializeSettings);
        }
    }
}
