using System;
using VPet_Simulator.Windows.Interface;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using LinePutScript.Localization.WPF;
using System.Windows;
using Panuon.WPF.UI;
using System.Windows.Threading;
using MenuItem = System.Windows.Controls.MenuItem;
using HorizontalAlignment = System.Windows.HorizontalAlignment;

namespace VPet.Plugin.LetsPlayIt
{
    public class LetsPlayIt : MainPlugin
    {
        public string path;
        public bool active = true;
        public double cooldown = 1;
        public bool showGameApp = true;
        public bool showMusicApp = true;
        public bool showPaintApp = true;

        public winSettings winSettings = null;
        public List<string> directoryPath = new List<string>();

        private Random random;
        private winApp winApp = null;
        private DispatcherTimer timer;
        private Dialogue dialogue = new Dialogue();
        private string[] typeNames = { "paintApps", "musicApps", "gameApps" };

        public List<string> gamePath = new List<string>();
        public List<string> musicPath = new List<string>();
        public List<string> paintPath = new List<string>();

        public List<string> gameApps = new List<string>();
        public List<string> musicApps = new List<string>();
        public List<string> paintApps = new List<string>();

        public override string PluginName => nameof(LetsPlayIt);

        public LetsPlayIt(IMainWindow mainwin)
          : base(mainwin)
        {
        }

        public override void LoadPlugin()
        {
            this.timer = new DispatcherTimer();
            this.timer.Tick += this.Timer_Tick;
            this.path = Directory.GetParent(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)).FullName;
            this.GetConfig();
            this.GetApps();
            this.GetAppsPath();
            this.CreateMenuItem();
        }

        public void GetConfig()
        {
            this.directoryPath.Clear();
            try
            {
                string[] lines = File.ReadAllLines(path + "\\config\\config.lps");
                if (lines.Length <= 0) return;

                foreach (string line in lines)
                {
                    string name = line.Split('=')[0];
                    string value = line.Split('=')[1];
                    if (name == null || value == null) continue;

                    if (name == "active")
                        this.active = bool.Parse(value);
                    else if (name == "cooldown")
                        this.cooldown = double.Parse(value);
                    else if (name == "showGameApp")
                        this.showGameApp = bool.Parse(value);
                    else if (name == "showMusicApp")
                        this.showMusicApp = bool.Parse(value);
                    else if (name == "showPaintApp")
                        this.showPaintApp = bool.Parse(value);
                    else if (name == "directoryPath")
                    {
                        if (value == "Desktop")
                            value = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                        if (value == "Programs")
                            value = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs");
                        this.directoryPath.Add(value);
                    }
                }

                this.timer.Stop();
                if (!this.active) return;
                this.timer.Interval = TimeSpan.FromHours(this.cooldown);
                this.timer.Start();
            }
            catch (IOException e)
            {
            }
        }

        public void GetApps()
        {
            this.paintApps.Clear();
            this.musicApps.Clear();
            this.gameApps.Clear();
            try
            {
                List<string>[] typeApps = { this.paintApps, this.musicApps, this.gameApps };

                for(int i = 0; i < typeApps.Length; i++)
                {
                    string[] lines = File.ReadAllLines(this.path + "\\config\\" + this.typeNames[i] + ".txt");
                    if (lines.Length <= 0) continue;

                    foreach (string line in lines)
                        typeApps[i].Add(line);
                }
            }
            catch (IOException e)
            {
            }
        }

        public void GetAppsPath()
        {
            this.paintPath.Clear();
            this.musicPath.Clear();
            this.gamePath.Clear();
            foreach (string directory in this.directoryPath)
            {
                if (!Directory.Exists(directory)) return;
                try
                {
                    FileAttributes attributes = File.GetAttributes(directory);
                    if ((attributes & FileAttributes.ReparsePoint) != FileAttributes.ReparsePoint)
                    {
                        string[] exeFiles = Directory.GetFiles(directory, "*.exe", SearchOption.TopDirectoryOnly)
                                            .Union(Directory.GetFiles(directory, "*.lnk", SearchOption.TopDirectoryOnly)).ToArray()
                                            .Union(Directory.GetFiles(directory, "*.url", SearchOption.TopDirectoryOnly)).ToArray();

                        if (exeFiles.Length <= 0) continue;
                        foreach (string exeFile in exeFiles)
                        {
                            string exeFileName = Path.GetFileNameWithoutExtension(exeFile);
                            if (this.gameApps.Contains(exeFileName))
                                this.gamePath.Add(exeFile);
                            else if (this.paintApps.Contains(exeFileName))
                                this.paintPath.Add(exeFile);
                            else if (this.musicApps.Contains(exeFileName))
                                this.musicPath.Add(exeFile);
                        }
                    }
                }
                catch (Exception ex) { }
            }
        }

        public void GetRandomApp(bool isTest)
        {
            List<string> randomTypeApps = new List<string>();
            if (this.gamePath.Count > 0 && this.showGameApp) randomTypeApps.Add("game");
            if (this.musicPath.Count > 0 && this.showMusicApp) randomTypeApps.Add("music");
            if (this.paintPath.Count > 0 && this.showPaintApp) randomTypeApps.Add("paint");

            if(!this.showGameApp && !this.showMusicApp && !this.showPaintApp)
            {
                if (isTest)
                    MessageBoxX.Show("Any type of application is disabled.".Translate(), nameof(LetsPlayIt).Translate());
                return;
            }

            if (randomTypeApps.Count <= 0)
            {
                if (isTest)
                    MessageBoxX.Show("No application found in the specified paths.".Translate(), nameof(LetsPlayIt).Translate());
                return;
            }

            string randomAppPath;
            string randomDialogue;

            this.random = new Random();
            int randomIndex = this.random.Next(randomTypeApps.Count);

            if (randomTypeApps[randomIndex] == "game")
            {
                randomAppPath = this.gamePath[this.random.Next(this.gamePath.Count)];
                randomDialogue = this.dialogue.game[this.random.Next(this.dialogue.game.Count)];
            }
            else if (randomTypeApps[randomIndex] == "music")
            {
                randomAppPath = this.musicPath[this.random.Next(this.musicPath.Count)];
                randomDialogue = this.dialogue.music[this.random.Next(this.dialogue.music.Count)];
            }
            else if (randomTypeApps[randomIndex] == "paint")
            {
                randomAppPath = this.paintPath[this.random.Next(this.paintPath.Count)];
                randomDialogue = this.dialogue.paint[this.random.Next(this.dialogue.paint.Count)];
            }
            else return;

            if (randomAppPath == null) return;
            else if (!File.Exists(randomAppPath))
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

            this.winApp.ChangePath(randomAppPath);
            this.SendMsg(randomDialogue.Translate().Replace("{APP}", Path.GetFileNameWithoutExtension(randomAppPath)));
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
            menuModConfig.Items.Add((object) menuItem);
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            this.GetRandomApp(false);
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