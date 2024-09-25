using System;
using VPet_Simulator.Windows.Interface;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using LinePutScript.Localization.WPF;
using System.Windows;
using Panuon.WPF.UI;
using System.Text.Json;
using System.Windows.Threading;
using MenuItem = System.Windows.Controls.MenuItem;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using Microsoft.Win32;
using VPet.Plugin.LetsPlayIt.Classes;

namespace VPet.Plugin.LetsPlayIt
{
    public class LetsPlayIt : MainPlugin
    {
        public string path;

        private Random random;
        private winApp winApp = null;
        public winSettings winSettings = null;
        private Dialogue dialogue = new Dialogue();

        public int time = 0;
        public DispatcherTimer timer;
        public DispatcherTimer cooldown;

        public List<string> gameAppNames = new List<string>();
        public List<string> musicAppNames = new List<string>();
        public List<string> artAppNames = new List<string>();

        public Config config = new Config();
        public List<AppInfo> appList = new List<AppInfo>();

        public override string PluginName => nameof(LetsPlayIt);

        public LetsPlayIt(IMainWindow mainwin)
          : base(mainwin)
        {
        }

        public override void LoadPlugin()
        {
            this.timer = new DispatcherTimer();
            this.timer.Tick += this.Timer_Tick;
            this.timer.Interval = TimeSpan.FromSeconds(1);
            this.timer.Start();

            this.cooldown = new DispatcherTimer();
            this.cooldown.Tick += this.Cooldown_Tick;
            this.path = Directory.GetParent(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)).FullName;

            this.LoadFromFile(Path.Combine(this.path, "config", "config.json"));
            this.RestartTimer();
            this.CreateMenuItem();
            this.LoadAppNames();
            this.LoadFromFile(Path.Combine(this.path, "config", "appList.json"));
            this.CheckPaths();
        }

        public void RestartTimer()
        {
            this.cooldown.Stop();
            if (!this.config.Active) return;
            this.time = 0;
            this.cooldown.Interval = TimeSpan.FromHours(this.config.Cooldown);
            this.cooldown.Start();
        }

        public void GetRandomApp(bool isTest = false)
        {
            List<AppType> types = new List<AppType>();
            if (this.config.ShowGameApps)
                types.Add(AppType.Game);
            if (this.config.ShowMusicApps)
                types.Add(AppType.Music);
            if (this.config.ShowArtApps)
                types.Add(AppType.Art);

            if (types.Count <= 0)
            {
                if (isTest)
                    MessageBoxX.Show("Any type of application is disabled.".Translate(), nameof(LetsPlayIt).Translate());
                return;
            }

            List<AppInfo> randomAppRange = this.appList.Where(e => e.Active).Where(el => types.Any(t => t == el.Type)).ToList();
            if (randomAppRange.Count <= 0)
            {
                if (isTest)
                    MessageBoxX.Show("No application found in the specified paths.".Translate(), nameof(LetsPlayIt).Translate());
                return;
            }

            this.random = new Random();
            AppInfo randomApp = randomAppRange[this.random.Next(randomAppRange.Count)];
            TriggerApp(randomApp, isTest);
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (!this.config.Active) return;

            this.time++;
            if (TimeSpan.FromSeconds(this.time) > this.cooldown.Interval)
                this.time = 1;

            if (this.winSettings != null)
                this.winSettings.UpdateTimer();
        }

        private void Cooldown_Tick(object sender, EventArgs e)
        {
            this.GetRandomApp();
        }

        public void TriggerApp(AppInfo appInfo, bool isTest = false)
        {
            this.random = new Random();
            List<string> dialogues = appInfo.Type == AppType.Game ? this.dialogue.game
                                 : appInfo.Type == AppType.Music ? this.dialogue.music
                                 : this.dialogue.art;
            string randomDialogue = dialogues[this.random.Next(dialogues.Count)];

            if (!File.Exists(appInfo.Path.Split("\" ")[0].Replace("\"", "")))
            {
                if (isTest)
                    MessageBoxX.Show("The application does not exist, it has probably been moved.".Translate(), nameof(LetsPlayIt).Translate());
                return;
            }

            if (this.winApp == null)
            {
                this.winApp = new winApp(this.MW);
                this.winApp.Visibility = Visibility.Visible;
                this.MW.Main.UIGrid.Children.Add(winApp);
            }

            this.winApp.ChangePath(appInfo);
            this.SendMsg(randomDialogue.Translate().Replace("{APP}", appInfo.Name));
        }

        public void StartUpLoad()
        {
            string filePath = Path.Combine(this.path, "config", "appList.json");
            bool pathExist = File.Exists(filePath);

            if (pathExist)
            {
                this.LoadFromFile(filePath);
                return;
            }

            if (MessageBox.Show("Do you want to load your Steam library?".Translate(), "First Load".Translate(), MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                this.GetSteamGames();

            if (MessageBox.Show("Do you want to search for apps from your ProgramFilesX86 (search by app name)?".Translate(), "First Load".Translate(), MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                this.ImportFolder(Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFilesX86));

            if (MessageBox.Show("Do you want to search for apps from your Desktop (search by app name)?".Translate(), "First Load".Translate(), MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                this.ImportFolder(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
        }

        private void LoadAppNames()
        {
            List<List<string>> lists = new List<List<string>> { this.gameAppNames, this.musicAppNames, this.artAppNames };
            List<string> fileNames = new List<string> { "gameAppNames.txt", "musicAppNames.txt", "artAppNames.txt" };

            for (int i = 0; i < lists.Count; i++)
            {
                lists[i].Clear();
                string path = Path.Combine(this.path, "config", fileNames[i]);
                if (File.Exists(path))
                    foreach (string line in File.ReadAllLines(path))
                        lists[i].Add(line);
            }
        }

        private void LoadFromFile(string filePath)
        {
            this.LoadAppNames();
            if (!File.Exists(filePath)) return;
            string json = File.ReadAllText(filePath);
            if (filePath.EndsWith("config.json"))
                this.config = JsonSerializer.Deserialize<Config>(json);
            else
                this.appList = JsonSerializer.Deserialize<List<AppInfo>>(json);
        }

        public void CheckPaths(bool showNotification = false)
        {
            if (this.appList.Count <= 0) return;

            List<AppInfo> appsToRemove = new List<AppInfo>();
            List<string> steamPaths = new List<string>();

            string key = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
            using (RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(key))
            {
                if (registryKey != null)
                    foreach (string subKeyName in registryKey.GetSubKeyNames())
                    {
                        if (!subKeyName.Contains("Steam App ")) continue;
                        using (RegistryKey subKey = registryKey.OpenSubKey(subKeyName))
                        {
                            if (subKey == null) continue;
                            object displayPath = subKey.GetValue("UninstallString");
                            if (displayPath == null) continue;
                            string steamPath = displayPath.ToString().Replace("uninstall", "run");
                            steamPaths.Add(steamPath);
                        }
                    }
            }

            foreach (AppInfo app in this.appList)
            {
                bool isSteam = app.Path.Contains(@"steam://run/");
                if ((!isSteam && !File.Exists(app.Path)) || (isSteam && !steamPaths.Contains(app.Path)))
                    appsToRemove.Add(app);
            }

            int count = appsToRemove.Count;
            foreach (AppInfo app in appsToRemove)
                this.appList.Remove(app);

            this.SaveToFile(Path.Combine(this.path, "config", "appList.json"));
            if (showNotification)
                if (count == 0)
                    MessageBox.Show("All paths match.".Translate(), nameof(LetsPlayIt).Translate());
                else
                    MessageBox.Show("Removed {0} apps without correct path.".Translate().Replace("{0}", count.ToString()), nameof(LetsPlayIt).Translate());
        }

        public void GetSteamGames()
        {
            this.LoadAppNames();
            List<AppInfo> tempList = new List<AppInfo>();
            string key = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
            using (RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(key))
            {
                if (registryKey == null) return;
                foreach (string subKeyName in registryKey.GetSubKeyNames())
                {
                    if (!subKeyName.Contains("Steam App ")) continue;
                    using (RegistryKey subKey = registryKey.OpenSubKey(subKeyName))
                    {
                        if (subKey == null) continue;
                        object displayIcon = subKey.GetValue("DisplayIcon");
                        object displayName = subKey.GetValue("DisplayName");
                        object displayPath = subKey.GetValue("UninstallString");
                        if (displayIcon == null || displayName == null || displayPath == null) continue;

                        try
                        {
                            string appPath = displayPath.ToString().Replace("uninstall", "run");
                            if (tempList.Any(x => x.Path == appPath) || this.appList.Any(x => x.Path == appPath))
                                continue;

                            AppType appType =
                                this.musicAppNames.Any(e => displayName.ToString().ToLower().Contains(e.ToLower())) ? AppType.Music
                                : this.artAppNames.Any(e => displayName.ToString().ToLower().Contains(e.ToLower())) ? AppType.Art
                                : AppType.Game;
                            tempList.Add(new AppInfo(0, displayIcon.ToString(), displayName.ToString(), appPath, appType, DateTime.Now.ToString("dd'/'MM'/'yy HH:mm")));
                        }
                        catch {}
                    }
                }
            }
            if (tempList.Count > 0)
                this.appList.AddRange(tempList);
            this.PrintLoaded(tempList);
        }

        public void ImportFile(string filePath)
        {
            this.LoadAppNames();
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            List<AppInfo> tempList = new List<AppInfo>();

            if (tempList.Any(x => x.Path == filePath) || this.appList.Any(x => x.Path == filePath))
                return;

            AppType appType =
                this.musicAppNames.Any(e => fileName.ToLower().Contains(e.ToLower())) ? AppType.Music
                : this.artAppNames.Any(e => fileName.ToLower().Contains(e.ToLower())) ? AppType.Art
                : AppType.Game;

            tempList.Add(new AppInfo(0, filePath, fileName, filePath, appType, DateTime.Now.ToString("dd'/'MM'/'yy HH:mm")));
 
            if (tempList.Count > 0)
                this.appList.AddRange(tempList);
            this.PrintLoaded(tempList);
        }

        public void ImportFolder(string folderPath)
        {
            string folderName = Path.GetFileName(folderPath);
            List<AppInfo> tempList = new List<AppInfo>();

            try
            {
                FileAttributes attributes = File.GetAttributes(folderPath);
                if ((attributes & FileAttributes.ReparsePoint) != FileAttributes.ReparsePoint)
                {
                    List<string> filesPath = new List<string>();
                        
                    foreach (string path in Directory.GetFiles(folderPath, "*.exe", SearchOption.TopDirectoryOnly)
                                            .Union(Directory.GetFiles(folderPath, "*.lnk", SearchOption.TopDirectoryOnly)).ToArray()
                                            .Union(Directory.GetFiles(folderPath, "*.url", SearchOption.TopDirectoryOnly)).ToArray())
                        filesPath.Add(path);

                    foreach (string folders in Directory.GetDirectories(folderPath))
                        foreach (string path in Directory.GetFiles(folders, "*.exe", SearchOption.TopDirectoryOnly)
                                                .Union(Directory.GetFiles(folders, "*.lnk", SearchOption.TopDirectoryOnly)).ToArray()
                                                .Union(Directory.GetFiles(folders, "*.url", SearchOption.TopDirectoryOnly)).ToArray())
                            filesPath.Add(path);

                    if (filesPath.Count > 0)
                        foreach (string filePath in filesPath)
                        {
                            string fileName = Path.GetFileNameWithoutExtension(filePath);
                            string filePath2 = filePath.Substring(0, filePath.Length - 3);
                            if (tempList.Any(x => x.Path == filePath) || this.appList.Any(x => x.Path == filePath))
                                continue;

                            AppType appType =
                                this.musicAppNames.Any(e => filePath2.ToLower().EndsWith($@"\{e.ToLower()}.")) ? AppType.Music
                                : this.artAppNames.Any(e => filePath2.ToLower().EndsWith($@"\{e.ToLower()}.")) ? AppType.Art
                                : this.gameAppNames.Any(e => filePath2.ToLower().EndsWith($@"\{e.ToLower()}.")) ? AppType.Game : AppType.None;

                            if (appType == AppType.None)
                                continue;

                            tempList.Add(new AppInfo(0, filePath, fileName, filePath, appType, DateTime.Now.ToString("dd'/'MM'/'yy HH:mm")));
                        }
                }
            }
            catch { }

            if (tempList.Count > 0)
                this.appList.AddRange(tempList);
            this.PrintLoaded(tempList);
        }

        private void PrintLoaded(List<AppInfo> tempList)
        {
            MessageBox.Show("Added {0} new games.".Translate().Replace("{0}", tempList.Where(x => x.Type == AppType.Game).ToArray().Length.ToString())
                + "\nAdded {1} new music apps.".Translate().Replace("{1}", tempList.Where(x => x.Type == AppType.Music).ToArray().Length.ToString())
                + "\nAdded {2} new art apps.".Translate().Replace("{2}", tempList.Where(x => x.Type == AppType.Art).ToArray().Length.ToString())
                , nameof(LetsPlayIt).Translate());
            this.SaveToFile(Path.Combine(this.path, "config", "appList.json"));
        }

        public void SaveToFile(string filePath)
        {
            string json = "";
            if (filePath.EndsWith("config.json"))
                json = JsonSerializer.Serialize(this.config, new JsonSerializerOptions { WriteIndented = true });
            else
                json = JsonSerializer.Serialize(this.appList, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }

        private void CreateMenuItem()
        {
            MenuItem menuModConfig = this.MW.Main.ToolBar.MenuMODConfig;
            menuModConfig.Visibility = Visibility.Visible;

            MenuItem menuItem = new MenuItem()
            {
                Header = nameof(LetsPlayIt).Translate(),
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            menuItem.Click += (RoutedEventHandler)((s, e) => this.Setting());
            menuModConfig.Items.Add((object)menuItem);
        }

        public override void Setting()
        {
            if (this.winSettings == null)
            {
                this.winSettings = new winSettings(this);
                this.winSettings.Show();
            }
            else
                this.winSettings.Topmost = true;
        }

        private void SendMsg(string msgContent)
        {
            this.MW.Main.Say(msgContent);
        }
    }
}