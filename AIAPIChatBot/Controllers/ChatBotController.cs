using System.Text;
using AI_API_ChatBot.Data;
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
                .FirstOrDefaultAsync(u => u.EmailAddress ==  Constants.AI_USER_EMAIL_ADDRESS);
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
                var requestBody = new
                {
                    model = "gpt-4o-mini",
                    messages = new[]
                    {
                        new
                        {
                            role = "system",
                            content = "You are a helpful assistant that answers questions based on the provided document. Answer questions accurately and concisely based only on the information in the document. If the question cannot be answered from the document, say so clearly."
                        },
                        new
                        {
                            role = "user",
                            content = $"Document content:\n{pdfText}\n\nQuestion: {question}"
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

                    //Save author (user) message
                    await _applicationDbContext.ChatMessages.AddAsync(new Data.Entities.ChatMessage
                    {
                        AuthorUserId = author.Id,
                        ReceipientUserId = aiUser.Id,
                        Message = request.Message.Trim(),
                        CreatedAt = DateTime.Now
                    });

                    //Save AI  message
                    await _applicationDbContext.ChatMessages.AddAsync(new Data.Entities.ChatMessage
                    {
                        AuthorUserId = aiUser.Id,
                        ReceipientUserId = author.Id,
                        Message = aiMessage.Trim(),
                        CreatedAt = DateTime.Now
                    });

                    await _applicationDbContext.SaveChangesAsync();

                    var authorMessages = await _applicationDbContext
                        .ChatMessages
                        .Where(m => m.AuthorUserId == author.Id || m.ReceipientUserId == author.Id)
                        .ToListAsync();

                    return Ok(authorMessages);

                }
                else
                {
                    return BadRequest($"API Error Code: {response.StatusCode} with Error Message: {responseContent}");
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
