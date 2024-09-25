using LinePutScript.Localization.WPF;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using VPet.Plugin.LetsPlayIt.Classes;

namespace VPet.Plugin.LetsPlayIt
{
    public partial class winSettings : Window
    {
        private int pageSize = 25;
        private string lastSort = "Name_ASC";
        private LetsPlayIt main;

        private bool hasLoad = false;

        public winSettings(LetsPlayIt main)
        {
            this.main = main;
            this.InitializeComponent();

            this.SetDataContent();
            this.main.StartUpLoad();
            this.UpdateAppList();
        }

        public void UpdateTimer()
        {
            TimeSpan i = this.main.cooldown.Interval;
            TimeSpan t = TimeSpan.FromSeconds(this.main.time);
            if (t <= i)
                this.Timer.Text = (i - t).ToString();
        }

        private void SetDataContent()
        {
            Config cfg = this.main.config;
            this.ActiveSwitch.IsChecked = cfg.Active;
            this.CooldownSlider.Value = cfg.Cooldown;
            this.ShowGameApp.IsChecked = cfg.ShowGameApps;
            this.ShowMusicApp.IsChecked = cfg.ShowMusicApps;
            this.ShowArtApp.IsChecked = cfg.ShowArtApps;
            this.hasLoad = true;
        }

        private void Test(object sender, RoutedEventArgs e) => this.main.GetRandomApp(true);

        private void OpenFile_G(object sender, RoutedEventArgs e)
        {
            this.OpenFile("\\config\\gameAppNames.txt");
        }

        private void OpenFile_P(object sender, RoutedEventArgs e)
        {
            this.OpenFile("\\config\\artAppNames.txt");
        }

        private void OpenFile_M(object sender, RoutedEventArgs e)
        {
            this.OpenFile("\\config\\musicAppNames.txt");
        }

        private void OpenFile(string fileName)
        {
            Process.Start(new ProcessStartInfo(this.main.path + fileName) { UseShellExecute = true });
        }

        public void ChangeAppCount()
        {
            this.AppFoundGame.Text = this.main.appList.Where(x => x.Active == true && x.Type == AppType.Game).ToArray().Length.ToString();
            this.AppFoundMusic.Text = this.main.appList.Where(x => x.Active == true && x.Type == AppType.Music).ToArray().Length.ToString();
            this.AppFoundArt.Text = this.main.appList.Where(x => x.Active == true && x.Type == AppType.Art).ToArray().Length.ToString();
        }

        private void SearchAppList(object sender, TextChangedEventArgs e)
        {
            this.UpdateAppList();
        }

        private void ChangeOrderBy(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            string sort = button.Tag.ToString();
            string[] args = sort.Split("_");
            button.Tag = $"{args[0]}_{(args[1] == "ASC" ? "DESC" : "ASC")}";
            this.UpdateAppList(sort);
        }

        private void UpdateAppList(string sort = "Name_ASC")
        {
            lastSort = sort;
            var args = sort.Split("_");
            if (!Enum.TryParse(args[0], true, out SortBy sortBy) || !Enum.TryParse(args[1], true, out SortType sortType))
                return;

            string search = this.SearchInput.Text;
            List<AppInfo> filteredList = this.main.appList.Where(e => e.Name.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();

            Func<AppInfo, object> sortFunc = sortBy switch
            {
                SortBy.Name => x => x.Name,
                SortBy.Path => x => x.Path,
                SortBy.Type => x => x.Type,
                SortBy.Date => x => x.Date,
                _ => x => x.Name
            };

            List<AppInfo> sortedList = sortType == SortType.ASC
                ? filteredList.OrderBy(sortFunc).ToList()
                : filteredList.OrderByDescending(sortFunc).ToList();

            int index = 1;
            this.LoadMoreButton.Visibility = Visibility.Collapsed;
            this.AppListControl.Items.Clear();

            foreach (AppInfo appInfo in sortedList.Take(pageSize))
            {
                appInfo.Index = index++;
                this.AppListControl.Items.Add(appInfo);
            }

            if (index < sortedList.Count)
                this.LoadMoreButton.Visibility = Visibility.Visible;

            this.ChangeAppCount();
        }

        private void ContextMenuApp(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            string path = ((ContextMenu)menuItem.Parent).Tag.ToString();
            string method = menuItem.Tag.ToString();

            if (method == "De/Activate")
                this.main.appList.First(x => x.Path == path).Active = !this.main.appList.First(x => x.Path == path).Active;
            else if (method == "Delete" || method == "Trigger")
            {
                AppInfo item = this.main.appList.FirstOrDefault(x => x.Path == path);
                if (item == null) return;
                if (method == "Trigger")
                {
                    this.main.TriggerApp(item, true);
                    return;
                }
                else
                    this.main.appList.Remove(item);
            }
            this.main.SaveToFile(Path.Combine(this.main.path, "config", "appList.json"));
            this.UpdateAppList(lastSort);
        }

        private void ChangeType(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            string type = menuItem.Tag.ToString();
            string path = ((ContextMenu)((MenuItem)menuItem.Parent).Parent).Tag.ToString();

            this.main.appList.First(x => x.Path == path).Type = type == "Game" ? AppType.Game : type == "Art" ? AppType.Art : AppType.Music;
            this.main.SaveToFile(Path.Combine(this.main.path, "config", "appList.json"));
            this.UpdateAppList(lastSort);
        }

        private void ClearAll(object sender, RoutedEventArgs e)
        {
            this.main.appList.Clear();
            this.main.SaveToFile(Path.Combine(this.main.path, "config", "appList.json"));
            this.UpdateAppList(lastSort);
        }

        private void ImportFrom(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            string method = menuItem.Tag.ToString();

            if (method == "Steam")
                this.main.GetSteamGames();
            else if (method == "File")
            {
                OpenFileDialog fileDialog = new OpenFileDialog
                {
                    Filter = "Select app".Translate() + " (*.exe, *.url, *.lnk)|*.exe; *.url; *.lnk",
                };
                bool? result = fileDialog.ShowDialog();
                if (result != true) return;

                string filePath = fileDialog.FileName;
                if (filePath == null) return;
                this.main.ImportFile(filePath);
            }
            else if (method == "Folder")
            {
                OpenFolderDialog folderDialog = new OpenFolderDialog();
                bool? result = folderDialog.ShowDialog();
                if (result != true) return;

                string folderPath = folderDialog.FolderName;
                if (folderPath == null) return;
                this.main.ImportFolder(folderPath);
            }

            this.main.SaveToFile(Path.Combine(this.main.path, "config", "appList.json"));
            this.UpdateAppList(lastSort);
        }

        private void OpenContextMenu(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            btn.ContextMenu.PlacementTarget = btn;
            btn.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            btn.ContextMenu.IsOpen = true;
        }

        private void ChangeSwitch(object sender, RoutedEventArgs e)
        {
            Panuon.WPF.UI.Switch sw = (Panuon.WPF.UI.Switch)sender;
            string method = sw.Tag.ToString();

            if (method == "Active")
            {
                this.main.config.Active = sw.IsChecked.Value;
                if (sw.IsChecked.Value)
                {
                    this.main.cooldown.Start();
                    this.main.RestartTimer();
                }
                else
                    this.main.cooldown.Stop();
            }
            else if (method == "Game")
                this.main.config.ShowGameApps = sw.IsChecked.Value;
            else if (method == "Art")
                this.main.config.ShowArtApps = sw.IsChecked.Value;
            else if (method == "Music")
                this.main.config.ShowMusicApps = sw.IsChecked.Value;

            this.main.SaveToFile(Path.Combine(this.main.path, "config", "config.json"));
        }

        private void ChangeCooldown(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!this.hasLoad) return;
            Slider sl = (Slider)sender;
            this.main.config.Cooldown = sl.Value;
            this.main.RestartTimer();
            this.main.SaveToFile(Path.Combine(this.main.path, "config", "config.json"));
        }

        private void LoadMorePage(object sender, RoutedEventArgs e)
        {
            this.pageSize += 25;
            this.UpdateAppList();
        }

        private void CheckApp(object sender, RoutedEventArgs e)
        {
            this.main.CheckPaths(true);
            this.UpdateAppList();
        }

        private void Window_Closed(object sender, EventArgs e) => this.main.winSettings = (winSettings)null;
    }
}