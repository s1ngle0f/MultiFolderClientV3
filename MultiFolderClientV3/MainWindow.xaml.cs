using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using Newtonsoft.Json;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MultiFolderClientV3
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        Synchronizer app;
        private string settingsPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/MultiFolder/settings.json";
        private void MyInit()
        {
            app = new Synchronizer();
            app.Start();
            UpdateLoginPassword();
            if (app.multifolder.IsExistUser())
            {
                this.login.Text = app.multifolder.login;
                //TurnOnFunctional();
                ShowDirsBlock();
            }
            else
            {
                //TurnOnChangeAccount();
            }
            ////DeleteDirsBlock();
            ////UpdateDirsBlock();
        }

        private void ShowDirsBlock()
        {
            var localDirs = getLocalDirs(app);
            var serverDirs = app.multifolder.GetDirs();
            var serverDirsAdd = HelpFunctions.CompareLists(localDirs.Select(path => System.IO.Path.GetFileName(path)).ToList(), serverDirs)["add"];
            AddLocalDirs(localDirs);
            AddServerDirs(serverDirsAdd);
        }

        private void AddLocalDirs(List<string> localDirs)
        {
            string xamlInfo = @"";
            
        }

        private void AddServerDirs(List<string> serverDirs)
        {
            
        }

        private void AddXAMLControlToControl<T>(string xamlControl, IAddChild control)
        {
            T newControl = (T)XamlReader.Parse(xamlControl);
            control.AddChild(newControl);
        }

        private List<string> getLocalDirs(Synchronizer app)
        {
            //List<string> localPaths = new List<string>();
            //foreach (DirectoryRepo dir in app.directories)
            //{
            //    localPaths.Add(dir.full_path);
            //}
            //return localPaths;
            using (var fileStream = new FileStream(settingsPath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                using (var streamReader = new StreamReader(fileStream))
                {
                    string json = streamReader.ReadToEnd();

                    Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                    List<string> dirPaths = JsonConvert.DeserializeObject<List<string>>(data["directories"].ToString());
                    return dirPaths;
                }
            }
        }

        private bool dirsContainsPath(string path, JArray dirs)
        {
            foreach (string dir in dirs)
            {
                if (System.IO.Path.GetFileName(path) == System.IO.Path.GetFileName(dir))
                    return true;
            }
            return false;
        }

        private void UpdateLoginPassword()
        {
            using (StreamReader sr = File.OpenText(settingsPath))
            {
                Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(sr.ReadToEnd());
                var login = (string)data["login"];
                this.login.Text = login;
                this.password.Text = "";
            }
        }

        public string ShowFolderBrowserDialog()
        {
            //using (var folderBrowserDialog = new FolderBrowserDialog())
            //{
            //    // Если нужно, задаем начальную папку
            //    // folderBrowserDialog.SelectedPath = "C:\\";

            //    // Открываем диалоговое окно выбора папки
            //    DialogResult result = folderBrowserDialog.ShowDialog();

            //    // Если пользователь выбрал папку, возвращаем ее путь
            //    if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(folderBrowserDialog.SelectedPath))
            //    {
            //        return folderBrowserDialog.SelectedPath;
            //    }
            //    else
            //    {
            //        return null;
            //    }
            //}

            return null; //Стереть потом!!!!!!
        }


    }
}
