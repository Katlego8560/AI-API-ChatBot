namespace AI_API_ChatBot.Data.Entities
{
    public class KnowledgeBase
    {
        public int Id { get; set; }
        public int AuthorId { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
