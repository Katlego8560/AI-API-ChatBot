using System.ComponentModel.DataAnnotations;

namespace AI_API_ChatBot.Models
{
    public class RegisterUserDto
    {
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [StringLength(200, ErrorMessage = "Email address cannot exceed 200 characters")]
        public required string EmailAddress { get; set; }
        [StringLength(100, ErrorMessage = "Email address cannot exceed 100 characters")]
        public required string FirstName { get; set; }
        [StringLength(100, ErrorMessage = "Email address cannot exceed 100 characters")]
        public required string LastName { get; set; }
    }
}
