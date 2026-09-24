using Microsoft.AspNetCore.Identity;

namespace KMC.EventClient.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
    }
}