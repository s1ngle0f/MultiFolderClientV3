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
using System.Windows.Media.Animation;
using System.Threading;

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

        Synchronizer _app;
        private string _settingsPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/MultiFolder/settings.json";
        private void MyInit()
        {
            _app = new Synchronizer();
            _app.Start();
            UpdateLoginPassword();
            if (_app.multifolder.IsExistUser())
            {
                this.login.Text = _app.multifolder.login;
                ShowDirsBlock();
            }
            ////DeleteDirsBlock();
            ////UpdateDirsBlock();
        }

        private void ShowDirsBlock()
        {
            var localDirs = GetLocalDirs(_app);
            var serverDirs = _app.multifolder.GetDirs();
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

        private void AddXamlControlToControl<T>(string xamlControl, IAddChild control)
        {
            T newControl = (T)XamlReader.Parse(xamlControl);
            control.AddChild(newControl);
        }

        private List<string> GetLocalDirs(Synchronizer app)
        {
            //List<string> localPaths = new List<string>();
            //foreach (DirectoryRepo dir in app.directories)
            //{
            //    localPaths.Add(dir.full_path);
            //}
            //return localPaths;
            using (var fileStream = new FileStream(_settingsPath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
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

        private bool DirsContainsPath(string path, JArray dirs)
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
            using (StreamReader sr = File.OpenText(_settingsPath))
            {
                Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(sr.ReadToEnd());
                var login = (string)data["login"];
                this.login.Text = login;
                this.password.Text = "";
            }
        }

        private void DeleteDirsBlock()
        {
            
        }

        private void UpdateDirsBlock()
        {
            if (_app.multifolder.IsExistUser())
            {
                DeleteDirsBlock();
                ShowDirsBlock();
            }
        }

        //public string ShowFolderBrowserDialog()
        //{
        //    using (var folderBrowserDialog = new FolderBrowserDialog())
        //    {
        //        // Если нужно, задаем начальную папку
        //        // folderBrowserDialog.SelectedPath = "C:\\";

        //        // Открываем диалоговое окно выбора папки
        //        DialogResult result = folderBrowserDialog.ShowDialog();

        //        // Если пользователь выбрал папку, возвращаем ее путь
        //        if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(folderBrowserDialog.SelectedPath))
        //        {
        //            return folderBrowserDialog.SelectedPath;
        //        }
        //        else
        //        {
        //            return null;
        //        }
        //    }
        //}

        private void editButton_Click(object sender, RoutedEventArgs e)
        {
            _app?.Stop();
            ChangeAccount(this.login.Text, this.password.Text);
            _app?.Start();
        }

        private void ChangeAccount(string login, string password)
        {
            if (_app.multifolder.IsExistUserByPassword(login, password))
            {
                SetNewLoginPassword(login, password);
                login_LostFocus();
                UpdateDirsBlock();
            }
            UpdateLoginPassword();
        }

        private void SetNewLoginPassword(string login, string password)
        {
            using (var fileStream = new FileStream(_settingsPath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                using (var streamReader = new StreamReader(fileStream))
                {
                    string json = streamReader.ReadToEnd();
                    dynamic data = JsonConvert.DeserializeObject(json);

                    string lastLogin = data.login;
                    string lastUserToken = data.userToken;

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
                    data.directories = data.history[login].directories;
                    data.login = login;
                    string userToken = _app.multifolder.Authorization(login, password);
                    data.userToken = userToken;
                    json = JsonConvert.SerializeObject(data, Formatting.Indented);
                    streamReader.BaseStream.SetLength(0);
                    streamReader.BaseStream.Position = 0;
                    var writer = new StreamWriter(streamReader.BaseStream);
                    writer.Write(json);
                    writer.Flush();

                    _app?.multifolder.SetLogin(login);
                    _app?.multifolder.SetUserToken(userToken);
                }
            }
        }

        private void login_GotFocus(object sender, RoutedEventArgs e)
        {
            if(stackPanelAnimation.Visibility == Visibility.Hidden)
            {
                // Показываем StackPanel с анимацией
                stackPanelAnimation.Visibility = Visibility.Visible;

                // Создаем анимацию для перемещения вниз
                DoubleAnimation slideAnimation = new DoubleAnimation
                {
                    From = -50,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(0.3),
                    FillBehavior = FillBehavior.Stop
                };

                // Применяем анимацию к вертикальному смещению StackPanel
                TranslateTransform transform = new TranslateTransform();
                stackPanelAnimation.RenderTransform = transform;
                transform.BeginAnimation(TranslateTransform.YProperty, slideAnimation);
            }
        }

        private void login_LostFocus()
        {
            // Создаем анимацию для перемещения вниз
            DoubleAnimation slideAnimation = new DoubleAnimation
            {
                From = 0,
                To = -50,
                Duration = TimeSpan.FromSeconds(0.3),
                FillBehavior = FillBehavior.Stop
            };

            // Применяем анимацию к вертикальному смещению StackPanel
            TranslateTransform transform = new TranslateTransform();
            stackPanelAnimation.RenderTransform = transform;
            transform.BeginAnimation(TranslateTransform.YProperty, slideAnimation);

            Thread thread = new Thread(() =>
            {
                Thread.Sleep(230);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    stackPanelAnimation.Visibility = Visibility.Hidden;
                });
            });

            thread.Start();
        }
    }
}
