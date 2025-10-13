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
    [ApiController]
    [Route("api/messages")]
    public class MessagesController : ControllerBase
    {
        private static readonly string API_KEY = "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";
        private static readonly string BASE_URL = "https://openrouter.ai/api/v1/chat/completions";
        private static readonly string PDF_DIR = "pdfs";
        private static readonly string DOCUMENT_NAME = "JAG-Lingo";
        private HttpClient httpClient = new HttpClient();
        private ApplicationDbContext _applicationDbContext { get; }

        public MessagesController(ApplicationDbContext applicationDbContext)
        {
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {API_KEY}");
            httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "http://localhost");
            httpClient.DefaultRequestHeaders.Add("X-Title", "PDF Q&A Chat App");

            _applicationDbContext = applicationDbContext;
        }


        [HttpPost("add-knowledge")]
        public async Task<IActionResult> Addknowledge([FromBody] AddKnowledgeDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest("Invalid payload provided");
                }

                var author = await _applicationDbContext.Users.FirstOrDefaultAsync(u => u.Id == request.UserId);

                if (author == null)
                {
                    return BadRequest("User not registred");
                }

                //TODO: Prevent Against SQL-Injection - sanitize the content before saving it in the database


                var newKnowledge = new KnowledgeBase
                {
                    AuthorId = author.Id,
                    Content = request.Content.Trim(),
                    CreatedAt = DateTime.Now
                };
                await _applicationDbContext.KnowledgeBase.AddAsync(newKnowledge);
                await _applicationDbContext.SaveChangesAsync();


                var authorKnowledgeBase = await _applicationDbContext.KnowledgeBase
                    .Where(m => m.AuthorId == author.Id)
                    .Select(k => new GetKnowledgeBaseDto
                    {
                        Id = k.Id,
                        AuthorId = author.Id,
                        Content = k.Content,
                        CreatedAt = k.CreatedAt.ToString("o")
                    })
                   .ToListAsync();


                return Ok(authorKnowledgeBase);

            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError.ToString(), "Internal server error occured");
                //return Content(HttpStatusCode.InternalServerError.ToString(), ex.Message);
            }
        }

        [HttpDelete("remove-knowledge")]
        public async Task<IActionResult> RemoveKnowledge([FromBody] RemoveKnowledgeBaseDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest("Invalid payload provided");
                }

                var knowledge = await _applicationDbContext.KnowledgeBase.FirstOrDefaultAsync(u => u.Id == request.Id);
                if (knowledge == null)
                {
                    return BadRequest("Knowledge Content not found");
                }

                var author = await _applicationDbContext.Users.FirstOrDefaultAsync(u => u.Id == request.UserId);
                if (author == null)
                {
                    return BadRequest("User not registred");
                }


                //Can only delete your own knowledge content
                if (knowledge.AuthorId != author.Id)
                {
                    return BadRequest("Cannot delete content not added by you");
                }

                _applicationDbContext.KnowledgeBase.Remove(knowledge);
                await _applicationDbContext.SaveChangesAsync();

                return Ok();

            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError.ToString(), "Internal server error occured");
                //return Content(HttpStatusCode.InternalServerError.ToString(), ex.Message);
            }
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

            string dataFolder = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "files");
            var files = Directory.GetFiles(dataFolder);
            if (files.Length == 0)
            {
                return BadRequest("No files found");
            }

            string filePath = files[0];
            string pdfText = ExtractTextFromPdf(filePath);
            if (string.IsNullOrWhiteSpace(pdfText))
            {
                return BadRequest("Error: Could not extract text from PDF. The file might be corrupted or password-protected.");
            }


            try
            {
                var usersknowledgeContent = string.Join(", ", await _applicationDbContext.KnowledgeBase
                                             .Where(k => k.AuthorId == author.Id)
                                             .Select(k => $"\"{k.Content}\"")
                                             .ToListAsync());

                var formattedContent = $"[{usersknowledgeContent}]";

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
                             content = $"Document content:\n{pdfText}\n\nAdditional context: {string.Join(". ", usersknowledgeContent)}\n\nQuestion: {question}"
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

        static string ExtractTextFromPdf(string filePath)
        {
            try
            {
                var sb = new StringBuilder();
                using (var reader = new PdfReader(filePath))
                {
                    for (int i = 1; i <= reader.NumberOfPages; i++)
                    {
                        string text = PdfTextExtractor.GetTextFromPage(reader, i);
                        sb.AppendLine(text);
                    }
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting text from PDF: {ex.Message}");
                return string.Empty;
            }
        }
    }
}
