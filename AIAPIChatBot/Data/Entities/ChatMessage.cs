namespace AI_API_ChatBot.Data.Entities
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public string Message { get; set; }
        public int AuthorUserId { get; set; }
        public int ReceipientUserId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
