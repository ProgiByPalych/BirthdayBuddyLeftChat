namespace BirthdayBuddyLeftChat.Models
{
    public class RestrictedUser
    {
        public required UserBirthday User { get; set; }
        public DateTime UnrestrictDate { get; set; }
    }
}
