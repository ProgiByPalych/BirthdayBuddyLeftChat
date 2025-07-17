using System.Text.Json;

namespace SemenNewsBot
{
    public class Settings
    {
        private static Settings? instance;
        public static Settings Instance
        {
            get
            {
                if (instance == null)
                    instance = new Settings();
                return instance;
            }
        }

        private static readonly string _settingsPath = "Settings.json";
        public string GetSettingsPath() { return _settingsPath; }
        public string? TokenToAccess { get; set; }
        public long SemenovChatId { get; set; }
        public int? SemenovThemeId { get; set; }

        public static void Init()
        {
            try
            {
                using (StreamReader reader = new StreamReader(Settings.Instance.GetSettingsPath()))
                {
                    string json = reader.ReadToEnd();
                    Settings.instance = JsonSerializer.Deserialize<Settings>(json);
                    Console.WriteLine("Read TokenToAccess: " + Settings.Instance.TokenToAccess);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Save();
            }
        }
        public static void Save()
        {
            // полная перезапись файла 
            using (StreamWriter writer = new StreamWriter(Settings.Instance.GetSettingsPath(), false))
            {
                writer.WriteLine(JsonSerializer.Serialize(Settings.Instance, typeof(Settings)));
            }
        }

        public static void Add()
        {
            // добавление в файл
            using (StreamWriter writer = new StreamWriter(Settings.Instance.GetSettingsPath(), true))
            {
                writer.WriteLine("Addition");
                writer.Write("4,5");
            }
        }
    }
}
