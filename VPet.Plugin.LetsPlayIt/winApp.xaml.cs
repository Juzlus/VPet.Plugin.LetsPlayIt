using System;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using VPet.Plugin.LetsPlayIt.Classes;
using VPet_Simulator.Core;
using VPet_Simulator.Windows.Interface;
using Path = System.IO.Path;

namespace VPet.Plugin.LetsPlayIt
{
    public partial class winApp : UserControl
    {
        private AppInfo activeApp;
        private IMainWindow main;
        private string missingIcon;

        public winApp(IMainWindow main)
        {
            this.main = main;
            this.InitializeComponent();
            this.Margin = new System.Windows.Thickness(0.0, 190.0, 0.0, 0.0);

            string path = Directory.GetParent(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)).FullName;
            missingIcon = Path.Combine(path, "missing_icon.png");
        }

        public async void ChangePath(AppInfo appInfo)
        {
            string lastPath = appInfo.Path;
            this.activeApp = appInfo;
            this.ConvertIconToImage();

            this.main.Main.DisplayToNomal();
            IGraph graph = this.main.Main.Core.Graph.FindGraph("letsplayit.clickme", GraphInfo.AnimatType.Single, IGameSave.ModeType.Happy);
            if (graph != null)
                this.main.Main.Display(graph);

            await Task.Delay(9800);
            if (lastPath != this.activeApp.Path) return;
            this.main.Main.DisplayToNomal();
            this.AppImage.Visibility = System.Windows.Visibility.Hidden;
        }

        private void ConvertIconToImage()
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();

                try
                {
                    if (this.activeApp.Path.Contains("steam://run/"))
                        bitmapImage.UriSource = new Uri(this.activeApp.Icon);
                    else
                    {
                        System.Drawing.Icon icon = System.Drawing.Icon.ExtractAssociatedIcon(this.activeApp.Path);
                        icon.ToBitmap().Save(memoryStream, ImageFormat.Png);
                        memoryStream.Seek(0, SeekOrigin.Begin);
                        bitmapImage.StreamSource = memoryStream;
                    }
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();
                    bitmapImage.Freeze();
                    this.AppImage.Source = bitmapImage;
                }
                catch { }
                this.AppImage.Visibility = System.Windows.Visibility.Visible;
            }
        }

        private void OpenApp(object sender, MouseButtonEventArgs e)
        {
            string appPath = this.activeApp.Path;
            string appArguments = "";

            if (appPath.Contains("steam://run/"))
            {
                string[] args = appPath.Split("\" ");
                appPath = args[0].Replace("\"", "");
                appArguments = args[1];
            }

            this.AppImage.Visibility = System.Windows.Visibility.Hidden;
            if (appPath == null || !File.Exists(appPath)) return;

            IGraph graph = this.main.Main.Core.Graph.FindGraph("letsplayit.clicked", GraphInfo.AnimatType.Single, IGameSave.ModeType.Happy);
            if (graph != null)
                this.main.Main.Display(graph, (Action)(() => this.main.Main.DisplayToNomal() ));

            Process.Start(new ProcessStartInfo {
                FileName = appPath,
                Arguments = appArguments,
                UseShellExecute = true 
            });
        }
    }
}
