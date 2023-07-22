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
using Microsoft.WindowsAPICodePack.Dialogs;

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
            DeleteDirsBlock();
            MyInit();
            // var localDirs = GetLocalDirs();
            // AddLocalDirs(localDirs);
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;

            // Устанавливаем позицию окна внизу справа
            this.Left = screenWidth - this.Width - 25;
            this.Top = screenHeight - this.Height - 60;
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

        private void UpdateLoginPassword()
        {
            using (StreamReader sr = File.OpenText(_settingsPath))
            {
                Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(sr.ReadToEnd());
                var login = (string)data["login"];
                this.login.Text = login;
                this.labelUserName.Content = login;
                this.password.Text = "";
            }
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

        private void UpdateDirsBlock()
        {
            if (_app.multifolder.IsExistUser())
            {
                DeleteDirsBlock();
                ShowDirsBlock();
            }
        }

        private void ShowDirsBlock()
        {
            var localDirs = GetLocalDirs();
            var serverDirs = _app.multifolder.GetDirs();
            var serverDirsAdd = HelpFunctions.CompareLists(localDirs.Select(path => System.IO.Path.GetFileName(path)).ToList(), serverDirs)["add"];
            AddLocalDirs(localDirs);
            AddServerDirs(serverDirsAdd);
        }

        private void DeleteDirsBlock()
        {
            this.localDirs.Children.Clear();
            this.serverDirs.Children.Clear();
        }

        private void AddLocalDirs(List<string> localDirs)
        {
            foreach (var localDir in localDirs)
            {
                string xamlInfo = "<Grid xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" " +
                                  "xmlns:x = \"http://schemas.microsoft.com/winfx/2006/xaml\" " +
                                  "xmlns:d = \"http://schemas.microsoft.com/expression/blend/2008\" " +
                                  "Margin =\"4\">" +
                                  $"    <Label VerticalAlignment=\"Center\" Content=\"{System.IO.Path.GetFileName(localDir)}\" />" +
                                  "    <Button" +
                                  "        Width=\"20\"" +
                                  "        Height=\"20\"" +
                                  "        Margin=\"0,0,3,0\"" +
                                  "        Padding=\"0,0,0,5\"" +
                                  "        HorizontalAlignment=\"Right\"" +
                                  "        VerticalAlignment=\"Center\"" +
                                  "        HorizontalContentAlignment=\"Center\"" +
                                  "        VerticalContentAlignment=\"Center\"" +
                                  $"        CommandParameter=\"{localDir}\"" +
                                  // "        Click=\"deleteMethod\"" +
                                  "        Content=\"✕\"" +
                                  "        Style=\"{StaticResource MaterialDesignFlatMidBgButton}\" />" +
                                  "</Grid>";
                AddXamlControlToControl<Grid>(xamlInfo, this.localDirs, DeleteMethod, localDir);
            }
        }

        private void AddServerDirs(List<string> serverDirs)
        {
            foreach (var serverDir in serverDirs)
            {
                string xamlInfo = "<Grid xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" " +
                                  "xmlns:x = \"http://schemas.microsoft.com/winfx/2006/xaml\" " +
                                  "xmlns:d = \"http://schemas.microsoft.com/expression/blend/2008\" " +
                                  "Margin =\"4\">" +
                                  $"    <Label VerticalAlignment=\"Center\" Content=\"{serverDir}\" />" +
                                  "    <Button" +
                                  "        Width=\"20\"" +
                                  "        Height=\"20\"" +
                                  "        Margin=\"0,0,3,0\"" +
                                  "        Padding=\"0,0,0,5\"" +
                                  "        HorizontalAlignment=\"Right\"" +
                                  "        VerticalAlignment=\"Center\"" +
                                  "        HorizontalContentAlignment=\"Center\"" +
                                  "        VerticalContentAlignment=\"Center\"" +
                                  $"        CommandParameter=\"{serverDir}\"" +
                                  // "        Click=\"addMethod\"" +
                                  "        Content=\"+\"" +
                                  "        Style=\"{StaticResource MaterialDesignFlatMidBgButton}\" />" +
                                  "</Grid>";
                AddXamlControlToControl<Grid>(xamlInfo, this.serverDirs, AddMethod, serverDir);
            }
        }

        private void AddXamlControlToControl<T>(string xamlControl, IAddChild control, Action<string> method = null, string commandParameter = null)
        {
            T newControl = (T) XamlReader.Parse(xamlControl);
            
            if (newControl is DependencyObject dependencyObject)
            {
                int count = VisualTreeHelper.GetChildrenCount(dependencyObject);
                for (int i = 0; i < count; i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(dependencyObject, i);
                    if (child is Button button)
                    {
                        button.Click += new RoutedEventHandler((sender, e) => method(commandParameter)); // Привязываем обработчик события к найденной кнопке
                    }
                }
            }

            control.AddChild(newControl);
        }

        private List<string> GetLocalDirs()
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

        public string ShowFolderBrowserDialog()
        {
            // using (var folderBrowserDialog = new FolderBrowserDialog())
            // {
            //     // Если нужно, задаем начальную папку
            //     // folderBrowserDialog.SelectedPath = "C:\\";
            //
            //     // Открываем диалоговое окно выбора папки
            //     DialogResult result = folderBrowserDialog.ShowDialog();
            //
            //     // Если пользователь выбрал папку, возвращаем ее путь
            //     if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(folderBrowserDialog.SelectedPath))
            //     {
            //         return folderBrowserDialog.SelectedPath;
            //     }
            //     else
            //     {
            //         return null;
            //     }
            // }
            using (CommonOpenFileDialog dialog = new CommonOpenFileDialog())
            {
                dialog.IsFolderPicker = true;

                // Открываем диалоговое окно выбора папки
                CommonFileDialogResult result = dialog.ShowDialog();

                // Проверяем результат выбора пользователя
                if (result == CommonFileDialogResult.Ok)
                {
                    // Получаем выбранную папку
                    string selectedPath = dialog.FileName;
                    // Далее можно использовать полученный selectedPath, чтобы обработать выбранную папку.
                    return selectedPath;
                }
                else
                {
                    return null;
                }
            }
        }

        private void login_GotFocus(object sender, RoutedEventArgs e)
        {
            if(stackPanelAnimation.Visibility == Visibility.Collapsed)
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
            if (stackPanelAnimation.Visibility == Visibility.Visible)
            {
                // Создаем анимацию для перемещения вверх
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
                        stackPanelAnimation.Visibility = Visibility.Collapsed;
                    });
                });

                thread.Start();
            }
        }

        private void AddMethod(string name)
        {
            // var name = ((Button)(sender)).CommandParameter.ToString();
            var path = System.IO.Path.Combine(ShowFolderBrowserDialog(), name);
            bool isWork = false;
            using (var fileStream = new FileStream(_settingsPath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                using (var streamReader = new StreamReader(fileStream))
                {
                    string json = streamReader.ReadToEnd();
                    dynamic data = JsonConvert.DeserializeObject(json);

                    if (!Directory.Exists(path) && !DirsContainsPath(path, data.directories))
                    {
                        data.directories.Add(path);
                        json = JsonConvert.SerializeObject(data, Formatting.Indented);
                        streamReader.BaseStream.SetLength(0);
                        streamReader.BaseStream.Position = 0;
                        var writer = new StreamWriter(streamReader.BaseStream);
                        writer.Write(json);
                        writer.Flush();
                        isWork = true;
                        this.localDirs.Children.Clear();
                        this.serverDirs.Children.Clear();
                    }
                }
            }
            if (isWork)
                ShowDirsBlock();
        }

        private void AddMethod()
        {
            // var name = ((Button)(sender)).CommandParameter.ToString();
            var path = ShowFolderBrowserDialog();
            bool isWork = false;
            using (var fileStream = new FileStream(_settingsPath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                using (var streamReader = new StreamReader(fileStream))
                {
                    string json = streamReader.ReadToEnd();
                    dynamic data = JsonConvert.DeserializeObject(json);

                    if (!Directory.Exists(path) && !DirsContainsPath(path, data.directories))
                    {
                        data.directories.Add(path);
                        json = JsonConvert.SerializeObject(data, Formatting.Indented);
                        streamReader.BaseStream.SetLength(0);
                        streamReader.BaseStream.Position = 0;
                        var writer = new StreamWriter(streamReader.BaseStream);
                        writer.Write(json);
                        writer.Flush();
                        isWork = true;
                        this.localDirs.Children.Clear();
                        this.serverDirs.Children.Clear();
                    }
                }
            }
            if (isWork)
                ShowDirsBlock();
        }

        private void DeleteMethod(string path)
        {
            // var path = ((Button)(sender)).CommandParameter.ToString();
            bool isWork = false;
            using (var fileStream = new FileStream(_settingsPath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                using (var streamReader = new StreamReader(fileStream))
                {
                    string json = streamReader.ReadToEnd();

                    Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                    List<string> dirPaths = JsonConvert.DeserializeObject<List<string>>(data["directories"].ToString());

                    for (int i = 0; i < dirPaths.Count; i++)
                    {
                        if (dirPaths[i] == path)
                        {
                            dirPaths.Remove(path);
                            data["directories"] = dirPaths;
                            json = JsonConvert.SerializeObject(data, Formatting.Indented);
                            streamReader.BaseStream.SetLength(0);
                            streamReader.BaseStream.Position = 0;
                            var writer = new StreamWriter(streamReader.BaseStream);
                            writer.Write(json);
                            writer.Flush();
                            isWork = true;
                            this.localDirs.Children.Clear();
                            this.serverDirs.Children.Clear();
                            break;
                        }
                    }
                }
            }
            if (isWork)
                ShowDirsBlock();
        }

        private void TestButton_OnClickButton_Click(object sender, RoutedEventArgs e)
        {
            //AddLocalDirs(new List<string>()
            //{
            //    "Hello",
            //    "Nigger"
            //});
            // this.labelUserName.Content = ((Button)(sender)).CommandParameter.ToString();
            // this.labelUserName.Content = ShowFolderBrowserDialog();
            // AddServerDirs(new List<string>()
            // {
            //     "Hello",
            //     "Nigger"
            // });
        }

        #region Перетаскивание_ПотеряФокуса

        // private void WindowTitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        // {
        //     if (e.ChangedButton == MouseButton.Left)
        //     {
        //         this.PlugForFocus.Focus();
        //         //Keyboard.ClearFocus();
        //         login_LostFocus();
        //         this.DragMove();
        //     }
        // }
        //
        // private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        // {
        //     if (e.ChangedButton == MouseButton.Left)
        //     {
        //         this.PlugForFocus.Focus();
        //         login_LostFocus();
        //         this.DragMove();
        //     }
        // }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.PlugForFocus.Focus();
                login_LostFocus();
                this.DragMove();
            }
        }

        #endregion

        private void ExitButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _app?.Stop(); //Потом удалить и переместить в Quit
            this.Close();
        }

        private void AddNewFolder_Click(object sender, RoutedEventArgs e)
        {
            AddMethod();
        }
    }
}
