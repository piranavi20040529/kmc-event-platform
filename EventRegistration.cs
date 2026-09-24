using System.ComponentModel.DataAnnotations.Schema;

namespace KMC.EventPlatformAPI.Models
{
    public class EventRegistration
    {
        public int Id { get; set; }
        public int EventId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public DateTime RegisteredAt { get; set; }

        [ForeignKey("EventId")]
        public virtual Event? Event { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}