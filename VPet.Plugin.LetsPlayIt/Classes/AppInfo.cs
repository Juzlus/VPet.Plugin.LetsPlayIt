namespace VPet.Plugin.LetsPlayIt.Classes
{
    public enum SortBy
    {
        Name,
        Path,
        Type,
        Date,
        Active
    }

    public enum SortType
    {
        ASC,
        DESC
    }

    public enum AppType
    {
        Game,
        Music,
        Art,
        None
    }

    public class AppInfo
    {
        public int Index { get; set; }
        public string Icon { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public AppType Type { get; set; }
        public string Date { get; set; }
        public bool Active { get; set; }

        public AppInfo() { }

        public AppInfo(int index, string icon, string name, string path, AppType appType, string date, bool active = true)
        {
            Index = index;
            Icon = icon;
            Name = name;
            Path = path;
            Type = appType;
            Date = date;
            Active = active;
        }
    }
}
