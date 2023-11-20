namespace Knowledge.Models.Chat
{
    public class ChatMessage
    {
        public string role { get; set; }
        public string content { get; set; }
        public TimeSpan? timespan { get; set; }
    }
}
