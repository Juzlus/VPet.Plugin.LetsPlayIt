namespace VPet.Plugin.LetsPlayIt.Classes
{
    public class Config
    {
        public bool Active { get; set; } = true;
        public double Cooldown { get; set; } = 2;
        public bool ShowGameApps { get; set; } = true;
        public bool ShowMusicApps { get; set; } = true;
        public bool ShowArtApps { get; set; } = true;

        public Config() { }
    }
}
