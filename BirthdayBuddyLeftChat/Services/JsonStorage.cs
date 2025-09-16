using System.Text.Json;
using BirthdayBuddyLeftChat.Models;

namespace BirthdayBuddyLeftChat.Services
{
    public class JsonStorage
    {
        private readonly string _filePath = "birthdays.json";
        private readonly string _restrictionsPath = "restrictions.json";

        public async Task<List<UserBirthday>> LoadBirthdaysAsync()
        {
            if (!File.Exists(_filePath)) return new List<UserBirthday>();

            var json = await File.ReadAllTextAsync(_filePath);
            return JsonSerializer.Deserialize<List<UserBirthday>>(json) ?? new List<UserBirthday>();
        }

        public async Task SaveBirthdaysAsync(List<UserBirthday> birthdays)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(birthdays, options);
            await File.WriteAllTextAsync(_filePath, json);
        }

        public async Task<List<RestrictedUser>> LoadRestrictionsAsync()
        {
            if (!File.Exists(_restrictionsPath)) return new List<RestrictedUser>();

            var json = await File.ReadAllTextAsync(_restrictionsPath);
            return JsonSerializer.Deserialize<List<RestrictedUser>>(json) ?? new List<RestrictedUser>();
        }

        public async Task SaveRestrictionsAsync(List<RestrictedUser> restrictions)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(restrictions, options);
            await File.WriteAllTextAsync(_restrictionsPath, json);
        }
    }
}
