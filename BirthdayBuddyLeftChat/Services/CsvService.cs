using BirthdayBuddyLeftChat.Models;
using CsvHelper;
using System.Globalization;

namespace BirthdayBuddyLeftChat.Services
{
    public class CsvService
    {
        // Экспорт: список пользователей → CSV (в виде строки)
        public async Task<string> ExportToCsvAsync(List<UserBirthday> birthdays)
        {
            using var memory = new MemoryStream();
            using var writer = new StreamWriter(memory);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

            csv.WriteRecords(birthdays);
            await writer.FlushAsync();

            return System.Text.Encoding.UTF8.GetString(memory.ToArray());
        }

        // Импорт: CSV-строка → список UserBirthday
        public async Task<List<UserBirthday>> ImportFromCsvAsync(string csvContent)
        {
            using var reader = new StringReader(csvContent);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            var records = await csv.GetRecordsAsync<UserBirthday>().ToListAsync();
            return records;
        }
    }
}
