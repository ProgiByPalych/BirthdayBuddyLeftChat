using BirthdayBuddyLeftChat.Models;
using SemenNewsBot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot;

namespace BirthdayBuddyLeftChat.Services
{
    public class BirthdayService
    {
        private static BirthdayService? instance;
        public static BirthdayService Instance
        {
            get
            {
                if (instance == null)
                    instance = new BirthdayService();
                return instance;
            }
        }

        private readonly JsonStorage _storage = new();

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

        public async Task StartDailyCheck(Func<long, string, Task> sendMessage, ITelegramBotClient botClient, CancellationToken ct)
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
                    await botClient.RestrictChatMember(
                        chatId: r.ChatId,
                        userId: r.UserId,
                        permissions: new ChatPermissions
                        {
                            CanAddWebPagePreviews = true,
                            CanSendMessages = true,
                            CanSendOtherMessages = true,
                            CanChangeInfo = true,
                            CanInviteUsers = true,
                            CanManageTopics = true,
                            CanPinMessages = true,
                            CanSendAudios = true,
                            CanSendVideos = true,
                            CanSendDocuments = true,
                            CanSendPhotos = true,
                            CanSendPolls = true,
                            CanSendVideoNotes = true,
                            CanSendVoiceNotes = true
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
                    await botClient.RestrictChatMember(
                        chatId: p.ChatId,
                        userId: p.UserId,
                        permissions: new ChatPermissions
                        {
                              CanAddWebPagePreviews = false,
                              CanSendMessages = false,
                              CanSendOtherMessages = false,
                              CanChangeInfo = false,
                              CanInviteUsers = true,
                              CanManageTopics = false,
                              CanPinMessages = false,
                              CanSendAudios = false,
                              CanSendVideos = false,
                              CanSendDocuments = false,
                              CanSendPhotos = false,
                              CanSendPolls = false,
                              CanSendVideoNotes = false,
                              CanSendVoiceNotes = false
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
                        await botClient.EditMessageText(
                            chatId: id,
                            messageId: msgId.Value,
                            text: text,
                            parseMode: ParseMode.Markdown,
                            cancellationToken: default);
                    }
                    else
                    {
                        var msg = await botClient.SendMessage(
                            chatId: id,
                            text: text,
                            parseMode: ParseMode.Markdown,
                            cancellationToken: default);

                        PinnedMessageIds[id] = msg.MessageId;

                        await botClient.PinChatMessage(
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

        public void EnsureChatExists(long chatId)
        {
            // Просто гарантируем, что чат "известен" — любая операция его создаст
            // Можно расширить: сохранять название чата, флаг активности и т.п.
        }

        public void AddOrUpdateUser(long chatId, long userId, string name)
        {
            var existing = _birthdays.FirstOrDefault(b => b.ChatId == chatId && b.UserId == userId);
            if (existing != null)
            {
                existing.Name = name; // Обновляем имя/ник
            }
            else
            {
                _birthdays.Add(new UserBirthday
                {
                    ChatId = chatId,
                    UserId = userId,
                    Name = name,
                    BirthDate = DateTime.Now, // заглушка
                    IsActive = true
                });
            }
        }
    }
}
