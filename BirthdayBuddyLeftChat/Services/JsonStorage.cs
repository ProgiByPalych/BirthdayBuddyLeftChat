using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using BirthdayBuddyLeftChat.Models;

namespace BirthdayBuddyLeftChat.Services
{
    public class JsonStorage
    {
        private readonly string _birthdaysPath = "data/birthdays.json";
        private readonly string _restrictionsPath = "data/restrictions.json";

        public JsonStorage()
        {
            Directory.CreateDirectory("data");
        }

        public async Task<List<T>> LoadAsync<T>(string path)
        {
            if (!File.Exists(path)) return new List<T>();

            var json = await File.ReadAllTextAsync(path, Encoding.UTF8);
            var options = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // 🔑 Разрешаем кириллицу без \u
                WriteIndented = true
            };

            return JsonSerializer.Deserialize<List<T>>(json, options) ?? new List<T>();
        }

        public async Task SaveAsync<T>(string path, List<T> data)
        {
            var options = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // 🔑 Отключаем экранирование Unicode
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(data, options);
            await File.WriteAllTextAsync(path, json, Encoding.UTF8); // Явно указываем UTF-8
        }

        // Удобные методы
        public Task<List<UserBirthday>> LoadBirthdays() =>
            LoadAsync<UserBirthday>(_birthdaysPath);

        public Task SaveBirthdays(List<UserBirthday> data) =>
            SaveAsync(_birthdaysPath, data);

        public Task<List<RestrictedUser>> LoadRestrictions() =>
            LoadAsync<RestrictedUser>(_restrictionsPath);

        public Task SaveRestrictions(List<RestrictedUser> data) =>
            SaveAsync(_restrictionsPath, data);
    }
}
