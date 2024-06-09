namespace Meet.Models
{
    public class ChatMessage
    {
        public string Username { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string AudioUrl { get; set; } // URL to the audio file
    }


}
