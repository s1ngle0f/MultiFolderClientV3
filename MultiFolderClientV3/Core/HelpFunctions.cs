using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Reflection;
using Newtonsoft.Json;

namespace MultiFolderClientV3
{
    public static class HelpFunctions
    {
        public static string Cmd(string command, bool IsPowerShell = false)
        {
            var fileName = IsPowerShell ? "powershell.exe" : "cmd.exe";
            var arguments = IsPowerShell ? $"-Command {command}" : $"/c {command}";
            // Настройка процесса
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true, // Перенаправляем вывод командной строки
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true, // Не создавать окно командной строки
                StandardOutputEncoding = Encoding.UTF8
            };

            // Запуск процесса
            Process process = new Process
            {
                StartInfo = psi
            };
            process.Start();

            // Получение вывода командной строки
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            // Дождитесь завершения процесса
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new Exception($"Process failed with code {process.ExitCode}. Error output: {error}");
            }

            return output;
        }

        public static string HashFile(string filePath, string hashAlgorithm = "SHA256")
        {
            string BytesToHexString(byte[] bytes)
            {
                StringBuilder sb = new StringBuilder();
                foreach (byte b in bytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
            using (var hashAlgorithmProvider = HashAlgorithm.Create(hashAlgorithm))
            {
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    byte[] hashBytes = hashAlgorithmProvider.ComputeHash(fileStream);
                    return BytesToHexString(hashBytes);
                }
            }
        }

        public static string ConvertToUtf8(string input)
        {
            // Кодировка, используемая для чтения исходных данных (возможно, она будет другой, если ваш процесс возвращает данные в другой кодировке)
            Encoding sourceEncoding = Encoding.Default;

            // Кодировка, в которую хотим преобразовать данные (UTF-8)
            Encoding targetEncoding = Encoding.UTF8;

            // Преобразуем строку в массив байтов в исходной кодировке
            byte[] sourceBytes = sourceEncoding.GetBytes(input);

            // Преобразуем массив байтов в строку в формате UTF-8
            string utf8String = targetEncoding.GetString(sourceBytes);

            return utf8String;
        }

        public static string GetExePath(string pathForTests = null)
        {
            if (pathForTests == null)
            {
                try
                {
                    if (Assembly.GetEntryAssembly() != null)
                        return Assembly.GetEntryAssembly()?.Location;

                    // Получаем путь к исполняемому файлу текущего процесса.
                    // string exePath = Process.GetCurrentProcess().MainModule.FileName;
                    string exePath = Process.GetCurrentProcess().MainModule.FileName;
                    return exePath;
                }
                catch (Exception ex)
                {
                    // Обработка исключения, если не удалось получить путь к исполняемому файлу.
                    // Здесь можно предоставить путь по умолчанию или выполнить другие действия в случае ошибки.
                    Console.WriteLine($"Ошибка при получении пути к исполняемому файлу: {ex.Message}");
                    return null;
                }
            }
            else
            {
                return pathForTests;
            }
        }

        public static void TransferFieldsFromOldJsonToNewJson(string lastSettingsPath, string newSettingsPath, List<string> keysFilter = null, bool reverseKeysFilter = false)
            //KeysFilter(без реверса) отвечает за то, какие значения ключей попадут из старого файла в новый
        {
            string newJsonData;
            using (StreamReader lastStreamReader = File.OpenText(lastSettingsPath))
            using (StreamReader newStreamReader = File.OpenText(newSettingsPath))
            {
                Dictionary<string, object> lastData = JsonConvert.DeserializeObject<Dictionary<string, object>>(lastStreamReader.ReadToEnd());
                Dictionary<string, object> newData = JsonConvert.DeserializeObject<Dictionary<string, object>>(newStreamReader.ReadToEnd());
                if (reverseKeysFilter && keysFilter != null)
                    keysFilter = lastData.Keys.ToList().Except(keysFilter).ToList();
                foreach (var lastDataKey in lastData.Keys)
                {
                    // Console.WriteLine($"\"{lastDataKey}\": {lastData[lastDataKey]}");
                    if (newData.ContainsKey(lastDataKey))
                    {
                        if (keysFilter != null && !keysFilter.Contains(lastDataKey))
                            continue;
                        // Console.WriteLine($"    {lastDataKey}: {lastData[lastDataKey]} -> {newData[lastDataKey]}");
                        newData[lastDataKey] = lastData[lastDataKey];
                    }
                }
                newJsonData = JsonConvert.SerializeObject(newData, Formatting.Indented);
            }
            File.WriteAllText(newSettingsPath, newJsonData);
        }

        public static void CombineJsonFilesWithPreservation(string lastSettingsPath, string newSettings, List<string> keysFilter = null, bool reverseKeysFilter = false)
            //KeysFilter(без реверса) отвечает за то, какие значения ключей попадут из старого файла в новый
        {
            string newJsonData;
            using (StreamReader lastStreamReader = File.OpenText(lastSettingsPath))
            {
                Dictionary<string, object> lastData = JsonConvert.DeserializeObject<Dictionary<string, object>>(lastStreamReader.ReadToEnd());
                Dictionary<string, object> newData = JsonConvert.DeserializeObject<Dictionary<string, object>>(newSettings);
                if (reverseKeysFilter && keysFilter != null)
                    keysFilter = lastData.Keys.ToList().Except(keysFilter).ToList();
                foreach (var lastDataKey in lastData.Keys)
                {
                    // Console.WriteLine($"\"{lastDataKey}\": {lastData[lastDataKey]}");
                    if (newData.ContainsKey(lastDataKey))
                    {
                        if (keysFilter != null && !keysFilter.Contains(lastDataKey))
                            continue;
                        // Console.WriteLine($"    {lastDataKey}: {lastData[lastDataKey]} -> {newData[lastDataKey]}");
                        newData[lastDataKey] = lastData[lastDataKey];
                        // newJsonData = JsonConvert.SerializeObject(newData, Formatting.Indented);
                    }
                }
                newJsonData = JsonConvert.SerializeObject(newData, Formatting.Indented);
            }
            File.WriteAllText(lastSettingsPath, newJsonData);
        }

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
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
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

        public static List<string> GetAllFilesDirectory(string targetDirectory, bool isRelativePath = true, List<string> files = null, string basePath = null)
        {
            if (Directory.Exists(targetDirectory))
            {
                if (files == null)
                {
                    files = new List<string>();
                    basePath = targetDirectory;
                }

                string[] fileEntries = Directory.GetFiles(targetDirectory);

                foreach (string fileName in fileEntries)
                {
                    string _fileName;
                    if (isRelativePath)
                        _fileName = GetRelativePath(basePath, fileName);
                    else
                        _fileName = fileName;
                    _fileName = _fileName.Replace("\\", "/").Replace("//", "/");
                    files.Add(_fileName);
                }

                string[] subdirectories = Directory.GetDirectories(targetDirectory);

                foreach (string subdirectory in subdirectories)
                {
                    GetAllFilesDirectory(subdirectory, isRelativePath, files, basePath);
                }

                return files;
            }

            return null;
        }

        public static string GetRelativePath(string basePath, string targetPath)
        {
            if (string.IsNullOrEmpty(basePath) || string.IsNullOrEmpty(targetPath))
                throw new ArgumentNullException("Both basePath and targetPath must be provided.");

            // Нормализация путей, замена слэшей на нужные разделители
            basePath = Path.GetFullPath(basePath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            targetPath = Path.GetFullPath(targetPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            // Разбиваем пути на отдельные части для анализа
            string[] baseDirectories = basePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string[] targetDirectories = targetPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            // Находим индекс первого различающегося элемента
            int commonIndex = 0;
            while (commonIndex < baseDirectories.Length && commonIndex < targetDirectories.Length
                && baseDirectories[commonIndex].Equals(targetDirectories[commonIndex], StringComparison.OrdinalIgnoreCase))
            {
                commonIndex++;
            }

            // Создаем относительный путь
            if (commonIndex == 0)
                return targetPath; // Пути полностью разные, невозможно найти общую часть.

            // Количество каталогов, которые нужно поднять, чтобы начать относительный путь
            int upCount = baseDirectories.Length - commonIndex;

            // Формируем массив из "../" для подъема на нужный уровень
            string[] relativePathParts = new string[upCount];
            for (int i = 0; i < upCount; i++)
                relativePathParts[i] = "..";

            // Добавляем к ним оставшуюся часть пути от targetPath
            string[] remainingParts = new string[targetDirectories.Length - commonIndex];
            Array.Copy(targetDirectories, commonIndex, remainingParts, 0, remainingParts.Length);

            // Объединяем все части в один путь с учетом разделителя
            string relativePath = Path.Combine(relativePathParts);
            if (remainingParts.Length > 0)
                relativePath = Path.Combine(relativePath, Path.Combine(remainingParts));

            return relativePath;
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
