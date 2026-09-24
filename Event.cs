using System.ComponentModel.DataAnnotations.Schema;

namespace KMC.EventPlatformAPI.Models
{
    public class Event
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Location { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public int MaxParticipants { get; set; }
        public string CreatedByUserId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsApproved { get; set; } = true;

        [ForeignKey("CreatedByUserId")]
        public virtual User? CreatedByUser { get; set; }
        public virtual ICollection<EventRegistration> Registrations { get; set; } = new List<EventRegistration>();
    }
}