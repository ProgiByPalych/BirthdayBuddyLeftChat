using SemenNewsBot;
using System.Collections.Concurrent;

namespace BirthdayBuddyLeftChat.Services
{
    public class BirthdayService
    {
        private readonly ConcurrentDictionary<string, DateTime> _birthdays = new();
        private readonly CsvService _csvService = new();

        public void AddBirthday(string name, DateTime birthDate)
        {
            _birthdays[name] = birthDate;
        }

        public List<(string Name, int Age)> GetBirthdaysToday()
        {
            var today = DateTime.Today;
            var results = new List<(string Name, int Age)>();

            foreach (var kvp in _birthdays)
            {
                var name = kvp.Key;
                var birthDate = kvp.Value;

                if (birthDate.Month == today.Month && birthDate.Day == today.Day)
                {
                    var age = today.Year - birthDate.Year;
                    results.Add((name, age));
                }
            }

            return results;
        }

        public async Task StartDailyCheck(Func<long, string, Task> sendMessage, TimeSpan interval, CancellationToken ct)
        {
            // Вычисляем время следующего запуска — завтра в 9:00
            var now = DateTime.Now;
            var nextRun = now.Date.AddDays(1).AddHours(9); // 9:00 утра
            if (now > nextRun)
                nextRun = nextRun.AddDays(1);

            var delay = nextRun - now;

            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(delay, ct);

                if (ct.IsCancellationRequested) break;

                try
                {
                    var birthdays = GetBirthdaysToday();
                    if (birthdays.Any())
                    {
                        var message = "🎂 Сегодня день рождения у:\n" +
                                      string.Join("\n", birthdays.Select(b => $"{b.Name} — {b.Age} лет!"));

                        await sendMessage(Settings.Instance.RootId, message);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при отправке напоминания: {ex.Message}");
                }

                delay = interval; // После первого раза — интервал по 24 часа
            }
        }

        public async Task<string> ExportBirthdaysToCsvAsync()
        {
            return await _csvService.ExportToCsvAsync(_birthdays);
        }

        public async Task<bool> ImportBirthdaysFromCsvAsync(string csvContent)
        {
            try
            {
                var imported = await _csvService.ImportFromCsvAsync(csvContent);
                foreach (var item in imported)
                {
                    AddBirthday(item.ChatId, item.UserId, item.Name, item.BirthDate);
                }
                await SaveDataAsync(); // Сохраняем в JSON
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
