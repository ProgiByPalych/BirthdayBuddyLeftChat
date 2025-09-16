namespace BirthdayBuddyLeftChat.Models
{
    public class RestrictedUser
    {
        public long ChatId { get; set; }
        public long UserId { get; set; }
        public DateTime UnrestrictDate { get; set; }
    }
}
