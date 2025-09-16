namespace BirthdayBuddyLeftChat.Models
{
    public class UserBirthday
    {
        public long ChatId { get; set; }
        public long UserId { get; set; }
        public string Name { get; set; } = "";
        public DateTime BirthDate { get; set; }
        public bool IsActive { get; set; } = true; // true = может писать
    }
}
