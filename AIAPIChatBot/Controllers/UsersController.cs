using AI_API_ChatBot.Data;
using AI_API_ChatBot.Data.Entities;
using AI_API_ChatBot.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace AI_API_ChatBot.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private ApplicationDbContext _applicationDbContext { get; }

        public UsersController(ApplicationDbContext applicationDbContext)
        {
            _applicationDbContext = applicationDbContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {

                var authorKnowledgeBase = await _applicationDbContext
                    .Users
                    .Select(u => new GetUserDto
                    {
                        Id = u.Id,
                        EmailAddress = u.EmailAddress,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        CreatedAt = u.CreatedAt.ToString("o")
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
        public async Task<IActionResult> Register([FromBody] RegisterUserDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest("Invalid payload provided");
                }

                var userInDb = await _applicationDbContext.Users
                   .FirstOrDefaultAsync(u => u.EmailAddress.ToLower() == request.EmailAddress.ToLower().Trim());

                
                if (userInDb != null)
                {
                
                    return Ok(new GetUserDto
                    {
                        Id = userInDb.Id,
                        EmailAddress = userInDb.EmailAddress,
                        FirstName = userInDb.FirstName,
                        LastName = userInDb.LastName,
                        CreatedAt = userInDb.CreatedAt.ToString("o")
                    });
                }

                if(request.EmailAddress.ToLower().Trim() == Constants.ADMIN_USER_EMAIL_ADDRESS.ToLower().Trim())
                {
                    return BadRequest("Cannot register with admin email address");
                }

                if (request.EmailAddress.ToLower().Trim() == Constants.AI_USER_EMAIL_ADDRESS.ToLower().Trim())
                {
                    return BadRequest("Cannot register with AI email address");
                }

                var newUser = new User
                {
                    EmailAddress = request.EmailAddress.Trim(),
                    FirstName = request.FirstName.Trim(),
                    LastName = request.LastName.Trim(),
                    CreatedAt = DateTime.Now
                };
                await _applicationDbContext.Users.AddAsync(newUser);
                await _applicationDbContext.SaveChangesAsync();

                var newRegisteredUser = await _applicationDbContext.Users.
                    FirstAsync(u => u.EmailAddress == newUser.EmailAddress);

                return Ok(new GetUserDto
                {
                    Id = newRegisteredUser.Id,
                    EmailAddress = newRegisteredUser.EmailAddress,
                    FirstName = newRegisteredUser.FirstName,
                    LastName = newRegisteredUser.LastName,
                    CreatedAt = newRegisteredUser.CreatedAt.ToString("o")
                });

            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError.ToString(), "Internal server error occured");
            }
        }
    }
}
