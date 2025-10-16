using AI_API_ChatBot.Data.Entities;
using AI_API_ChatBot.Data;
using AI_API_ChatBot.Models;
using System.Net.Http;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AI_API_ChatBot.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class KnowledgeController : ControllerBase
    {
        private ApplicationDbContext _applicationDbContext { get; }

        public KnowledgeController(ApplicationDbContext applicationDbContext)
        {
            _applicationDbContext = applicationDbContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                
                var authorKnowledgeBase = await _applicationDbContext
                    .KnowledgeBase
                    .Select(k => new GetKnowledgeBaseDto
                    {
                        Id = k.Id,
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

        [HttpPost]
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
                    return BadRequest("Author not registered.");
                }

                if (author.EmailAddress != Constants.ADMIN_USER_EMAIL_ADDRESS)
                {
                    return BadRequest("Only admins can add knowledge content");
                }

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

        [HttpDelete]
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
                    return BadRequest("Author not registered.");
                }

                if (author.EmailAddress != Constants.ADMIN_USER_EMAIL_ADDRESS)
                {
                    return BadRequest("Only admins can remove knowledge content");
                }

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

            }
        }
    }
}
