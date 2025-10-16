using System.Net;
using System.Text;
using AI_API_ChatBot.Data;
using AI_API_ChatBot.Data.Entities;
using AI_API_ChatBot.Models;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace AI_API_ChatBot.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MessagesController : ControllerBase
    {
        private static readonly string API_KEY = "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";
        private static readonly string BASE_URL = "https://openrouter.ai/api/v1/chat/completions";
        private HttpClient httpClient = new HttpClient();
        private ApplicationDbContext _applicationDbContext { get; }

        public MessagesController(ApplicationDbContext applicationDbContext)
        {
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {API_KEY}");
            httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "http://localhost");
            httpClient.DefaultRequestHeaders.Add("X-Title", "PDF Q&A Chat App");

            _applicationDbContext = applicationDbContext;
        }
       
        [HttpPost]
        public async Task<IActionResult> ReceiveMessage([FromBody] SendMessageDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid payload");
            }

            var author = await _applicationDbContext.Users
                .FirstOrDefaultAsync(u => u.Id == request.UserId);

            if (author == null)
            {
                return BadRequest("User not registred");
            }

            var aiUser = await _applicationDbContext.Users
                .FirstOrDefaultAsync(u => u.EmailAddress == Constants.AI_USER_EMAIL_ADDRESS);
            if (aiUser == null)
            {
                return BadRequest("AI User not found. Contact support");
            }

            var question = request.Message.Trim();

           


            try
            {
                var adminKnowledgeContent = string.Join(", ", await _applicationDbContext.KnowledgeBase
                                             .Where(k => k.AuthorId == author.Id)
                                             .Select(k => $"\"{k.Content}\"")
                                             .ToListAsync());

                var formattedContent = $"[{adminKnowledgeContent}]";

                var requestBody = new
                {
                    model = "gpt-4o-mini",
                    messages = new[]
                    {
                        new
                        {
                            role = "system",
                           content = $"You are a helpful assistant that answers questions based on the provided document and additional context from the user's knowledge base. When answering questions, first prioritize information from the document. If the question cannot be answered from the document, use the additional context to provide an answer. If the question still cannot be answered, say so clearly."
                        },
                        new
                        {
                            role = "user",
                             content = $"Context: {string.Join(". ", adminKnowledgeContent)}\n\nQuestion: {question}"
                        }
                    },
                    max_tokens = 1000,
                    temperature = 0.7
                };

                string jsonContent = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(BASE_URL, content);
                string responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var responseObj = JsonConvert.DeserializeObject<dynamic>(responseContent);
                    string aiMessage = responseObj?.choices?[0]?.message?.content?.ToString() ?? "No response received.";


                    await _applicationDbContext.ChatMessages.AddAsync(new Data.Entities.ChatMessage
                    {
                        AuthorUserId = author.Id,
                        ReceipientUserId = aiUser.Id,
                        Message = request.Message.Trim(),
                        CreatedAt = DateTime.Now
                    });


                    await _applicationDbContext.ChatMessages.AddAsync(new ChatMessage
                    {
                        AuthorUserId = aiUser.Id,
                        ReceipientUserId = author.Id,
                        Message = aiMessage.Trim(),
                        CreatedAt = DateTime.Now
                    });

                    await _applicationDbContext.SaveChangesAsync();

                    var recentMessages = await _applicationDbContext
                        .ChatMessages
                        .Where(m => m.AuthorUserId == author.Id || m.ReceipientUserId == author.Id)
                        .OrderByDescending(m => m.CreatedAt)
                        .Take(2)
                        .OrderBy(m => m.CreatedAt)
                        .ToListAsync();

                    return Ok(recentMessages);

                }
                else
                {
                    return BadRequest($"AI API Integration Error Code: {response.StatusCode} with Error Message: {responseContent}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calling OpenRouter API: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
            }

            return Ok(new { ReceivedMessage = request.Message });
        }

    }
}