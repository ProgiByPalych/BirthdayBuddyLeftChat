using System.Text.Json;

namespace BirthdayBuddyLeftChat
{
    /// <summary>
    /// Глобальные настройки
    /// </summary>
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
        public string GetSettingsPath() => _settingsPath;
        /// <summary>
        /// Токен для подключения бота
        /// </summary>
        public string? TokenToAccess { get; set; }
        /// <summary>
        /// Id владельца бота
        /// </summary>
        public long RootId { get; set; } = 0;

        /// <summary>
        /// Инициализация настроек из файла
        /// </summary>
        public static void Init()
        {
            try
            {
                if (!File.Exists(_settingsPath))
                {
                    // Создаём дефолтный файл
                    Save();
                    Console.WriteLine($"Файл настроек создан: {_settingsPath}. Заполните его и перезапустите.");
                    Environment.Exit(1);
                }

                string json = File.ReadAllText(_settingsPath);
                var settings = JsonSerializer.Deserialize<Settings>(json);

                if (settings == null)
                    throw new Exception("Не удалось десериализовать Settings.json");

                instance = settings;

                Console.WriteLine("Настройки загружены: Token=" + (string.IsNullOrEmpty(settings.TokenToAccess) ? "null" : "*****"));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке настроек: {ex.Message}");
                Console.WriteLine("Создаю новый файл...");
                Save();
                Environment.Exit(1);
            }
        }

        /// <summary>
        /// Сохраняет текущие настройки в файл
        /// </summary>
        public static void Save()
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(Instance, options);
            File.WriteAllText(_settingsPath, json);
            Console.WriteLine($"Файл настроек сохранён: {_settingsPath}");
        }
    }
}
