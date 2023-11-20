namespace Knowledge.Models.Chat
{
    public class ChatSession
    {
        public string UserId { get; set; }
        public string SessionId { get; set; }
        public TimeSpan? Started { get; set; }
        public TimeSpan? LastMessage { get; set; }
    }
}
