namespace AI_API_ChatBot.Models
{
    public class AddKnowledgeDto
    {
        public required int UserId { get; set; }
        public required string Content { get; set; }
    }
}
