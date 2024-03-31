using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace VPet.Plugin.LetsPlayIt
{
    public partial class winSettings : Window
    {
        private LetsPlayIt main;

        public winSettings(LetsPlayIt main)
        {
            this.main = main;
            this.InitializeComponent();
            this.SetDataContent();
        }

        private void SetDataContent()
        {
            this.ActiveSwitch.IsChecked = this.main.active;
            this.CooldownSlider.Value = this.main.cooldown;
            this.ShowGameApp.IsChecked = this.main.showGameApp;
            this.ShowMusicApp.IsChecked = this.main.showMusicApp;
            this.ShowPaintApp.IsChecked = this.main.showPaintApp;

            TextBox[] textBoxArray = new TextBox[3]
            {
                this.Path_1,
                this.Path_2,
                this.Path_3
            };

            for (int i = 0; i < this.main.directoryPath.Count; ++i)
                textBoxArray[i].Text = this.main.directoryPath[i] == null ? "" : this.main.directoryPath[i].ToString();

            this.ChangeAppCount();
        }

        private void Save(object sender, RoutedEventArgs e)
        {
            string contents =
                $"active={this.ActiveSwitch.IsChecked.ToString()}" +
                $"\ncooldown={this.CooldownSlider.Value.ToString()}" +
                $"\nshowGameApp={this.ShowGameApp.IsChecked.ToString()}" +
                $"\nshowMusicApp={this.ShowMusicApp.IsChecked.ToString()}" +
                $"\nshowPaintApp={this.ShowPaintApp.IsChecked.ToString()}";

            if (!string.IsNullOrEmpty(this.Path_1.Text))
                contents += $"\ndirectoryPath={this.Path_1.Text}";
            if (!string.IsNullOrEmpty(this.Path_2.Text))
                contents += $"\ndirectoryPath={this.Path_2.Text}";
            if (!string.IsNullOrEmpty(this.Path_3.Text))
                contents += $"\ndirectoryPath={this.Path_3.Text}";

            try
            {
                File.WriteAllText($"{this.main.path}\\config\\config.lps", contents);
                this.main.GetConfig();
                this.main.GetApps();
                this.main.GetAppsPath();
                this.ChangeAppCount();
            }
            catch (Exception ex) { }
        }

        private void Test(object sender, RoutedEventArgs e) => this.main.GetRandomApp(true);

        public void ChangeAppCount()
        {
            this.AppFoundGame.Text = this.main.gamePath.Count.ToString();
            this.AppFoundMusic.Text = this.main.musicPath.Count.ToString();
            this.AppFoundPaint.Text = this.main.paintPath.Count.ToString();
        }

        private void OpenFile_G(object sender, RoutedEventArgs e)
        {
            this.OpenFile("\\config\\gameApps.txt");
        }

        private void OpenFile_P(object sender, RoutedEventArgs e)
        {
            this.OpenFile("\\config\\paintApps.txt");
        }

        private void OpenFile_M(object sender, RoutedEventArgs e)
        {
            this.OpenFile("\\config\\musicApps.txt");
        }

        private void OpenFile(string fileName)
        {
            Process.Start(new ProcessStartInfo(this.main.path + fileName) { UseShellExecute = true });
        }

        private void Window_Closed(object sender, EventArgs e) => this.main.winSettings = (winSettings)null;
    }
}