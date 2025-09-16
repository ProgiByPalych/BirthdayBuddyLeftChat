using BirthdayBuddyLeftChat.Models;
using SemenNewsBot;
using System.Collections.Concurrent;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot;

namespace BirthdayBuddyLeftChat.Services
{
    public class BirthdayService
    {
        private readonly JsonStorage _storage = new();
        private readonly CsvService _csvService = new();

        public List<UserBirthday> _birthdays = new();
        public List<RestrictedUser> _restrictions = new();
        public Dictionary<long, int?> PinnedMessageIds { get; } = new();

        public async Task LoadDataAsync()
        {
            _birthdays = await _storage.LoadBirthdays();
            _restrictions = await _storage.LoadRestrictions();
        }

        public async Task SaveDataAsync()
        {
            await _storage.SaveBirthdays(_birthdays);
            await _storage.SaveRestrictions(_restrictions);
        }

        public void AddBirthday(long chatId, long userId, string name, DateTime birthDate)
        {
            var existing = _birthdays.FirstOrDefault(b => b.ChatId == chatId && b.UserId == userId);
            if (existing != null) _birthdays.Remove(existing);

            _birthdays.Add(new UserBirthday
            {
                ChatId = chatId,
                UserId = userId,
                Name = name,
                BirthDate = birthDate,
                IsActive = true
            });
        }

        public List<UserBirthday> GetBirthdaysToday()
        {
            var today = DateTime.Today;
            return _birthdays.Where(b =>
                b.BirthDate.Month == today.Month &&
                b.BirthDate.Day == today.Day).ToList();
        }

        public string GenerateUpcomingBirthdaysText(long chatId)
        {
            var today = DateTime.Today;
            var birthdaysInChat = _birthdays
                .Where(b => b.ChatId == chatId)
                .OrderBy(b => (b.BirthDate.Month - 1) * 31 + b.BirthDate.Day)
                .ToList();

            var lines = new List<string> { "🎂 **Предстоящие дни рождения**" };
            lines.Add("———");

            var upcoming = birthdaysInChat
                .Where(b =>
                {
                    var nextBirthday = new DateTime(today.Year, b.BirthDate.Month, b.BirthDate.Day);
                    if (nextBirthday < today) nextBirthday = nextBirthday.AddYears(1);
                    return (nextBirthday - today).Days <= 30;
                })
                .Select(b =>
                {
                    var next = new DateTime(today.Year, b.BirthDate.Month, b.BirthDate.Day);
                    if (next < today) next = next.AddYears(1);
                    var daysLeft = (next - today).Days;
                    var age = next.Year - b.BirthDate.Year;
                    return $"{b.Name} — {next:dd.MM} ({(daysLeft == 0 ? "сегодня" : $"{daysLeft} дн.")}), {age} лет";
                })
                .ToList();

            if (upcoming.Any())
            {
                lines.AddRange(upcoming);
            }
            else
            {
                lines.Add("🎉 В ближайшее время нет дней рождений.");
            }

            lines.Add("———");
            lines.Add("_Обновлено автоматически_");

            return string.Join("\n", lines);
        }

        public async Task StartDailyCheck(
            Func<long, string, Task> sendMessage,
            ITelegramBotClient botClient,
            CancellationToken ct)
        {
            await LoadDataAsync();

            var now = DateTime.Now;
            var nextRun = now.Date.AddDays(1).AddHours(9); // 9:00
            if (now > nextRun) nextRun = nextRun.AddDays(1);

            var delay = nextRun - now;

            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(delay, ct);
                if (ct.IsCancellationRequested) break;

                try
                {
                    await ProcessDailyEvents(botClient, sendMessage);
                    await SaveDataAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка: {ex.Message}");
                    await sendMessage(Settings.Instance.RootId, $"❌ Ошибка в ежедневной проверке: {ex.Message}");
                }

                delay = TimeSpan.FromHours(24);
            }
        }

        private async Task ProcessDailyEvents(ITelegramBotClient botClient, Func<long, string, Task> sendMessage)
        {
            var today = DateTime.Today;

            // Разблокировка
            var toUnrestrict = _restrictions.Where(r => r.UnrestrictDate <= today).ToList();
            foreach (var r in toUnrestrict)
            {
                try
                {
                    await botClient.RestrictChatMemberAsync(
                        chatId: r.ChatId,
                        userId: r.UserId,
                        permissions: new ChatPermissions
                        {
                            CanSendMessages = true,
                            CanSendMediaMessages = true,
                            CanSendOtherMessages = true,
                            CanAddWebPagePreviews = true
                        },
                        cancellationToken: default);

                    var user = _birthdays.FirstOrDefault(u => u.ChatId == r.ChatId && u.UserId == r.UserId);
                    if (user != null) user.IsActive = true;

                    _restrictions.Remove(r);
                    await sendMessage(r.ChatId, $"🎉 {user?.Name} снова может писать!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Разблокировка: {ex.Message}");
                }
            }

            // Дни рождения
            var birthdays = GetBirthdaysToday();
            foreach (var p in birthdays)
            {
                var until = today.AddDays(3);

                try
                {
                    await botClient.RestrictChatMemberAsync(
                        chatId: p.ChatId,
                        userId: p.UserId,
                        permissions: new ChatPermissions
                        {
                            CanSendMessages = false,
                            CanSendMediaMessages = false,
                            CanSendOtherMessages = false,
                            CanAddWebPagePreviews = false
                        },
                        untilDate: DateTime.UtcNow.AddDays(3),
                        cancellationToken: default);

                    _restrictions.Add(new RestrictedUser
                    {
                        ChatId = p.ChatId,
                        UserId = p.UserId,
                        UnrestrictDate = until
                    });

                    var age = today.Year - p.BirthDate.Year;
                    await sendMessage(p.ChatId, $"🤫 {p.Name} временно отключён.\n🎁 Готовим сюрприз?");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ограничение: {ex.Message}");
                }
            }

            // Обновление шапки
            var chatIds = _birthdays.Select(b => b.ChatId).Distinct();
            foreach (var id in chatIds)
            {
                try
                {
                    var text = GenerateUpcomingBirthdaysText(id);
                    var msgId = PinnedMessageIds.GetValueOrDefault(id);

                    if (msgId.HasValue)
                    {
                        await botClient.EditMessageTextAsync(
                            chatId: id,
                            messageId: msgId.Value,
                            text: text,
                            parseMode: ParseMode.Markdown,
                            cancellationToken: default);
                    }
                    else
                    {
                        var msg = await botClient.SendTextMessageAsync(
                            chatId: id,
                            text: text,
                            parseMode: ParseMode.Markdown,
                            cancellationToken: default);

                        PinnedMessageIds[id] = msg.MessageId;

                        await botClient.PinChatMessageAsync(
                            chatId: id,
                            messageId: msg.MessageId,
                            disableNotification: true,
                            cancellationToken: default);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Шапка чата {id}: {ex.Message}");
                    PinnedMessageIds[id] = null;
                }
            }
        }

        public async Task<string> ExportToCsvAsync(long chatId)
        {
            var data = _birthdays.Where(b => b.ChatId == chatId).ToList();
            return await _csvService.ExportToCsvAsync(data);
        }

        public async Task<bool> ImportFromCsvAsync(string csvContent)
        {
            try
            {
                var imported = await _csvService.ImportFromCsvAsync(csvContent);
                foreach (var item in imported)
                {
                    AddBirthday(item.ChatId, item.UserId, item.Name, item.BirthDate);
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Импорт CSV: " + ex.Message);
                return false;
            }
        }
    }
}
