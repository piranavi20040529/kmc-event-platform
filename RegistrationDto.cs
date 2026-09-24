using System.ComponentModel.DataAnnotations;

namespace KMC.EventPlatformAPI.Models
{
    public class RegisterDto
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;
        public string? Role { get; set; } = "User";
    }
}