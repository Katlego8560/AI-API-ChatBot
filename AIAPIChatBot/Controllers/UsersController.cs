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


        [HttpPost]
        public async Task<IActionResult> Register([FromBody] RegisterUserDto request)
        {

            try
            {

                if (!ModelState.IsValid)
                {
                    return BadRequest("Invalid payload provided");
                }

                var newUser = new User
                {
                    EmailAddress = request.EmailAddress.Trim(),
                    FirstName = request.FirstName.Trim(),
                    LastName = request.LastName.Trim()
                };

                await _applicationDbContext.Users.AddAsync(newUser);
                await _applicationDbContext.SaveChangesAsync();

                var userInDb = await _applicationDbContext.Users.FirstOrDefaultAsync(u => u.EmailAddress == newUser.EmailAddress);
                return Ok(userInDb);

            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError.ToString(), "Internal server error occured");
                //return Content(HttpStatusCode.InternalServerError.ToString(), ex.Message);
            }
        }
    }
}
