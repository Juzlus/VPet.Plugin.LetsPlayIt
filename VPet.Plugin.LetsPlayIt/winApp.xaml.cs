using System;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using VPet_Simulator.Core;
using VPet_Simulator.Windows.Interface;

namespace VPet.Plugin.LetsPlayIt
{
    public partial class winApp : UserControl
    {
        private string appPath;
        private IMainWindow main;

        public winApp(IMainWindow main)
        {
            this.main = main;
            this.InitializeComponent();
            this.Margin = new System.Windows.Thickness(0.0, 190.0, 0.0, 0.0);
        }

        public async void ChangePath(string path)
        {
            this.appPath = path;
            string lastPath = path;
            this.ConvertIconToImage();

            this.main.Main.DisplayToNomal();
            IGraph graph = this.main.Main.Core.Graph.FindGraph("letsplayit.clickme", GraphInfo.AnimatType.Single, IGameSave.ModeType.Happy);
            if (graph != null)
                this.main.Main.Display(graph);

            await Task.Delay(9800);
            if (lastPath != this.appPath) return;
            this.main.Main.DisplayToNomal();
            this.AppImage.Visibility = System.Windows.Visibility.Hidden;
        }

        private void ConvertIconToImage()
        {
            System.Drawing.Icon icon = System.Drawing.Icon.ExtractAssociatedIcon(this.appPath);

            using (MemoryStream memoryStream = new MemoryStream())
            {
                icon.ToBitmap().Save(memoryStream, ImageFormat.Png);
                memoryStream.Seek(0, SeekOrigin.Begin);

                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memoryStream;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                this.AppImage.Source = bitmapImage;
                this.AppImage.Visibility = System.Windows.Visibility.Visible;
            }
        }

        private void OpenApp(object sender, MouseButtonEventArgs e)
        {
            this.AppImage.Visibility = System.Windows.Visibility.Hidden;

            if (this.appPath == null) return;
            if (!File.Exists(this.appPath)) return;

            IGraph graph = this.main.Main.Core.Graph.FindGraph("letsplayit.clicked", GraphInfo.AnimatType.Single, IGameSave.ModeType.Happy);
            if (graph != null)
            {
                this.main.Main.Display(graph, (Action)(() =>
                {
                    this.main.Main.DisplayToNomal();
                }));
            }

            Process.Start(new ProcessStartInfo(this.appPath) { UseShellExecute = true });
        }
    }
}
