namespace AI_API_ChatBot.Models
{
    public class SendMessageDTO
    {
        public required int UserId { get; set; }
        public required string Message { get; set; }
    }
}
