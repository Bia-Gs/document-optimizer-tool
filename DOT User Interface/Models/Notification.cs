namespace DOT_User_Interface.Models
{
    public class Notification
    {
        public string? Title { get; set; }
        public required string Message { get; set; }
        public DateTime DateCreated { get; set; }
        public bool IsRead { get; set; }
    }
}
