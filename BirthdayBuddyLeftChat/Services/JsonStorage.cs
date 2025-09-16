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

            var json = await File.ReadAllTextAsync(path);
            return JsonSerializer.Deserialize<List<T>>(json) ?? new List<T>();
        }

        public async Task SaveAsync<T>(string path, List<T> data)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(data, options);
            await File.WriteAllTextAsync(path, json);
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
