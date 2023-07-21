using System;
using System.IO;
using System.Web;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using System.Diagnostics;
using Newtonsoft.Json.Linq;

namespace MultiFolderClientV3
{
    public class MultiFolder
    {
        // private string basic_url = "http://localhost:5000/";
        private static string ip_addr = "multifolder.ru";
        private string basic_url = $"https://{ip_addr}/";

        public string login;
        public string userToken;
        public string email = "";
        private Dictionary<string, string> auth;

        public MultiFolder(string login, string userToken)
        {
            this.login = login;
            this.email = login;
            this.userToken = userToken;
            auth = new Dictionary<string, string> { { "login", login }, { "userToken", userToken } };
        }

        public void SetLogin(string login)
        {
            this.login = login;
        }

        public void SetUserToken(string userToken)
        {
            this.userToken = userToken;
        }

        public string GenerateSshRemote(string server_dir_name)
        {
            return $"ssh://{login}@{ip_addr}/~/git/{server_dir_name}.git";
        }

        public string Ping()
        {
            WebClient client = new WebClient();
            client.Encoding = System.Text.Encoding.GetEncoding("UTF-8");
            var response = client.DownloadString($"{basic_url}ping?login={login}&usertoken={userToken}");
            return response;
        }

        public bool IsExistUser(string login, string userToken) // Немного кривая проверка, возможно, переделать
        {
            WebClient client = new WebClient();
            client.Encoding = System.Text.Encoding.GetEncoding("UTF-8");
            var response = client.DownloadString($"{basic_url}is_exist_user?login={login}&usertoken={userToken}");
            return bool.Parse(response);
        }

        public bool IsExistUserByPassword(string login, string password) // Немного кривая проверка, возможно, переделать
        {
            WebClient client = new WebClient();
            client.Encoding = System.Text.Encoding.GetEncoding("UTF-8");
            var response = client.DownloadString($"{basic_url}is_exist_user_by_password?login={login}&password={password}");
            return bool.Parse(response);
        }

        public bool IsExistUser() // Немного кривая проверка, возможно, переделать
        {
            WebClient client = new WebClient();
            client.Encoding = System.Text.Encoding.GetEncoding("UTF-8");
            var response = client.DownloadString($"{basic_url}is_exist_user?login={login}&usertoken={userToken}");
            return bool.Parse(response);
        }

        public List<string> GetDirs()
        {
            WebClient client = new WebClient();
            client.Encoding = System.Text.Encoding.GetEncoding("UTF-8");
            var response = client.DownloadString($"{basic_url}get_dirs?login={login}&usertoken={userToken}");
            return JArray.Parse(response).ToObject<List<string>>();
        }

        public string AddDir(string server_dir_name)
        {
            WebClient client = new WebClient();
            client.Encoding = System.Text.Encoding.GetEncoding("UTF-8");
            var response = client.DownloadString($"{basic_url}add_dir?login={login}&usertoken={userToken}&dir_name={server_dir_name}");
            return response;
        }

        public string DeleteDir(string server_dir_name)
        {
            WebClient client = new WebClient();
            client.Encoding = System.Text.Encoding.GetEncoding("UTF-8");
            var response = client.DownloadString($"{basic_url}delete_dir?login={login}&usertoken={userToken}&dir_name={server_dir_name}");
            return response;
        }

        public string AddSshKey(string ssh_key)
        {
            WebClient client = new WebClient();
            client.Encoding = System.Text.Encoding.GetEncoding("UTF-8");
            var response = client.DownloadString($"{basic_url}add_ssh_key?login={login}&usertoken={userToken}&ssh_key={HttpUtility.UrlEncode(ssh_key)}");
            return response;
        }

        public void Registrate(string password)
        {
            WebClient client = new WebClient();
            client.Encoding = System.Text.Encoding.GetEncoding("UTF-8");
            client.DownloadString($"{basic_url}registrate?login={login}&password={password}");
        }

        public List<string> GetListWorkingFiles()
        {
            WebClient client = new WebClient();
            client.Encoding = System.Text.Encoding.GetEncoding("UTF-8");
            var response = client.DownloadString($"{basic_url}get_list_working_files");
            return JToken.Parse(response)["files"].ToObject<List<string>>();
        }

        public void GetWorkingFile(string file_name, string file_path)
        {
            WebClient client = new WebClient();
            client.Encoding = System.Text.Encoding.GetEncoding("UTF-8");
            client.DownloadFile($"{basic_url}get_working_file?file_name={file_name}", file_path);
        }

        public string Authorization(string login, string password)
        {
            WebClient client = new WebClient();
            client.Encoding = System.Text.Encoding.GetEncoding("UTF-8");
            var response = client.DownloadString($"{basic_url}authorization_desktop?login={login}&password={password}");
            return response.Replace("\"", "");
        }
    }
}
