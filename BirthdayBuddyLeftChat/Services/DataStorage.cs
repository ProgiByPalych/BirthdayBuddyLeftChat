using BirthdayBuddyLeftChat.Models;
using System.Collections.Concurrent;

namespace BirthdayBuddyLeftChat.Services
{
    public class DataStorage
    {
        private static DataStorage? _instance;
        public static DataStorage Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new DataStorage();
                }
                return _instance;
            }
        }

        private readonly JsonStorage _storage = new();
        private List<UserBirthday> _birthdays = new();
        private List<RestrictedUser> _restrictions = new();
        private ConcurrentDictionary<long, int?> _pinnedMessageIds = new();
        private Dictionary<long, List<UserBirthday>>? _currentListFutureBirthDay;
        private Dictionary<long, List<UserBirthday>>? _lastListFutureBirthDay;

        public List<UserBirthday> Birthdays {  get { return _birthdays; } }
        public long[] ChatIds { get { return _birthdays.Select(b => b.ChatId).Distinct().ToArray(); } }

        /// <summary>
        /// Изменился ли список предстоящих именинников?
        /// </summary>
        /// <param name="chatId"></param>
        /// <returns></returns>
        public bool IsNewBirthdaysList(long chatId)
        {
            bool state = false;
            if (_currentListFutureBirthDay == null) { _currentListFutureBirthDay = new(); state = true; }
            if (_lastListFutureBirthDay == null) { _lastListFutureBirthDay = new(); state = true; }
            if (state) return true;
            return _currentListFutureBirthDay[chatId].Count == _lastListFutureBirthDay[chatId].Count && _currentListFutureBirthDay[chatId].ToHashSet().SetEquals(_lastListFutureBirthDay[chatId]);
        }

        public void SaveBirthdaysList(long chatId)
        {
            if (_lastListFutureBirthDay != null)
                _lastListFutureBirthDay[chatId] = new(_currentListFutureBirthDay == null ? new() : _currentListFutureBirthDay[chatId]);
        }

        public string GenerateUpcomingBirthdaysText(long chatId, int daysAhead = 15)
        {
            if (_currentListFutureBirthDay == null) _currentListFutureBirthDay = new();
            else if (!_currentListFutureBirthDay.ContainsKey(chatId)) _currentListFutureBirthDay.Add(chatId, new());
            else _currentListFutureBirthDay[chatId].Clear();

            DateTime today = DateTime.Today;
            // Получим все дни рождения для текущего чата и отсортируем.
            var birthdaysInChat = Instance.GetUsersByChatId(chatId);

            var lines = new List<string> { "🎂 **Предстоящие дни рождения**" };
            lines.Add("———");

            var upcoming = birthdaysInChat
                .Where(b =>
                {
                    DateTime nextBirthday = new DateTime(today.Year, b.BirthDate.Month, b.BirthDate.Day);
                    if (nextBirthday < today) nextBirthday = nextBirthday.AddYears(1);
                    return (nextBirthday - today).Days <= daysAhead;
                })
                .Select(b =>
                {
                    _currentListFutureBirthDay[chatId].Add(b);

                    DateTime next = new DateTime(today.Year, b.BirthDate.Month, b.BirthDate.Day);
                    if (next < today) next = next.AddYears(1);
                    int daysLeft = (next - today).Days;
                    int age = next.Year - b.BirthDate.Year;
                    int lastDigit = age % 10;
                    string year;
                    if (lastDigit == 1) year = "год";
                    else if (lastDigit > 1 && lastDigit < 5) year = "года";
                    else year = "лет";
                    return $"{b.GetFullName()}\n{next:dd.MM} ({(daysLeft == 0 ? "сегодня" : (daysLeft == 1 ? "завтра" : $"{daysLeft} дн."))}), {age} {year}.";
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
            lines.Add($"_Обновлено {today:dd.MM} автоматически_");

            return string.Join("\n", lines);
        }

        public List<UserBirthday> GetUsersByChatId(long chatId)
        {
            return _birthdays.Where(b => b.ChatId == chatId).ToList();
        }

        public UserBirthday? GetUserByUserName(long chatId, string userName)
        {
            return _birthdays.FirstOrDefault(b => b.ChatId == chatId && b.UserName.Equals(userName, StringComparison.OrdinalIgnoreCase));
        }

        public UserBirthday? GetUserByUrlSafeId(string urlSafeId)
        {
            return _birthdays.FirstOrDefault(b => b.UrlSafeId == urlSafeId);
        }

        public void AddBirthday(UserBirthday user)
        {
            if (user.UserId != 0)
            {
                var existing = _birthdays.FirstOrDefault(b => b.ChatId == user.ChatId && b.UserId == user.UserId);
                if (existing != null) _birthdays.Remove(existing);
            }
            else
            {
                var existing = _birthdays.FirstOrDefault(b => b.GetFullName() == user.GetFullName() && b.BirthDate == user.BirthDate);
                if (existing != null) _birthdays.Remove(existing);
            }

            _birthdays.Add(user);
        }

        public int? GetPinnedMsgId(long chatId)
        {
            return _pinnedMessageIds.GetValueOrDefault(chatId);
        }

        public void SetPinnedMsgId(long chatId, int msgId)
        {
            _pinnedMessageIds[chatId] = msgId;
        }

        public void SortBirthday()
        {
            _birthdays = new List<UserBirthday>(_birthdays.OrderBy(b => (b.BirthDate.Month - 1) * 31 + b.BirthDate.Day).ToList());
        }

        public void LoadDataAsync()
        {
            _birthdays = _storage.LoadBirthdays();
            SortBirthday();
            foreach (UserBirthday user in _birthdays)
            {
                if (user.IdBase64 == string.Empty) user.Id = GuidGenerator.GuidRandom();
            }
            //_restrictions = await _storage.LoadRestrictions();
        }

        public async Task SaveBirthdayDataAsync()
        {
            await _storage.SaveBirthdays(_birthdays);
            //await _storage.SaveRestrictions(_restrictions);
        }
    }
}
