using AI_API_ChatBot.Models;
using Microsoft.AspNetCore.Mvc;

namespace AI_API_ChatBot.Controllers
{
    [ApiController]
    [Route("api/messages")]
    public class MessagesController : ControllerBase
    {
        [HttpPost]
        public IActionResult ReceiveMessage([FromBody] MessageDTO request)
        {
            if (request == null || string.IsNullOrEmpty(request.Message))
            {
                return BadRequest("Message is required.");
            }

            return Ok(new { ReceivedMessage = request.Message });
        }
    }
}
