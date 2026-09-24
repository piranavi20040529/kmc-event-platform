using Microsoft.AspNetCore.Identity;

namespace KMC.EventPlatformAPI.Models
{
    public class User : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? Role { get; set; } = "Public";

        public virtual ICollection<Event> CreatedEvents { get; set; } = new List<Event>();
        public virtual ICollection<EventRegistration> Registrations { get; set; } = new List<EventRegistration>();
    }
}