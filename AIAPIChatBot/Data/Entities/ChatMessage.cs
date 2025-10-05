namespace AI_API_ChatBot.Data.Entities
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public string Message { get; set; }
        public virtual User User { get; set; }
        public int UserId { get; set; }
    }
}
