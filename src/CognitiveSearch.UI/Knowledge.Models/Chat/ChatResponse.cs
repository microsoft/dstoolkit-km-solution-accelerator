namespace Knowledge.Models.Chat
{
    public class ChatResponse
    {
        public string answer { get; set; }
        public IEnumerable<string> followUpQs { get; set; } 
        public IEnumerable<ChatReference> references { get; set; }
    }

    public class ChatReference
    {
        public string name { get; set; }
        public string page { get; set; }
        public string parentId { get; set; }
        public string chunkId { get; set; }
        public string url { get; set; }
        public bool isAbsoluteUrl { get; set; }
    }
}
