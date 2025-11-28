namespace BirthdayBuddyLeftChat.Models
{
    /// <summary>
    /// Объект человек
    /// </summary>
    public class UserBirthday
    {
        /// <summary>
        /// Принадлежит чату
        /// </summary>
        public long ChatId { get; set; }
        /// <summary>
        /// Идентификатор человека
        /// </summary>
        public long UserId { get; set; }
        /// <summary>
        /// Уникальное имя пользователя
        /// </summary>
        public string UserName { get; set; } = "";
        /// <summary>
        /// Фамилия
        /// </summary>
        public string LastName { get; set; } = "";
        /// <summary>
        /// Имя
        /// </summary>
        public string FirstName { get; set; } = "";
        /// <summary>
        /// Отчество
        /// </summary>
        public string Patronymic { get; set; } = "";
        /// <summary>
        /// Дата рождения
        /// </summary>
        public DateTime BirthDate { get; set; }
        /// <summary>
        /// Доступ к чату, может писать/читать
        /// </summary>
        public bool IsActive { get; set; } = true;
        /// <summary>
        /// Вычислить возраст
        /// </summary>
        /// <returns>Количество исполняющихся лет</returns>
        public int GetAge()
        {
            DateTime today = DateTime.Today;
            int age = today.Year - BirthDate.Year;
            if (BirthDate.Month > today.Month || (BirthDate.Month == today.Month && BirthDate.Day > today.Day))
            {
                age--;
            }
            return age;
        }
        /// <summary>
        /// Получить ФИО. Возвращает строку, содержащую фамилию, имя и отчество в правильном порядке.
        /// Пропускает отсутствующие части и избегает лишних пробелов.
        /// Примеры:
        ///   "Иванов Иван Иванович"
        ///   "Иванов Иван"
        ///   "Иван"
        ///   "Петров Иванович"
        /// </summary>
        /// <returns>ФИО как строка, или пустая строка, если все поля пустые</returns>
        public string GetFullName()
        {
            List<string> parts = new List<string>();

            if (!string.IsNullOrEmpty(LastName)) parts.Add(LastName);
            if (!string.IsNullOrEmpty(FirstName)) parts.Add(FirstName);
            if (!string.IsNullOrEmpty(Patronymic)) parts.Add(Patronymic);

            return string.Join(" ", parts);
        }

        #region Equality Members

        public override bool Equals(object? obj)
        {
            return Equals(obj as UserBirthday);
        }

        public bool Equals(UserBirthday? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return string.Equals(LastName, other.LastName, StringComparison.Ordinal) &&
                   string.Equals(FirstName, other.FirstName, StringComparison.Ordinal) &&
                   string.Equals(Patronymic, other.Patronymic, StringComparison.Ordinal) &&
                   BirthDate.Date == other.BirthDate.Date; // Сравниваем только дату, без времени
        }

        public override int GetHashCode()
        {
            // Используем HashCode.Combine (доступен начиная с .NET Core 2.1 / .NET Standard 2.1)
            return HashCode.Combine(
                LastName?.ToLowerInvariant(),
                FirstName?.ToLowerInvariant(),
                Patronymic?.ToLowerInvariant(),
                BirthDate.Date
            );
        }

        public static bool operator ==(UserBirthday? left, UserBirthday? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(UserBirthday? left, UserBirthday? right)
        {
            return !Equals(left, right);
        }

        #endregion
    }
}
