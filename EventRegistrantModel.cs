using System;

namespace KMC.EventClient.Models
{
    public class EventRegistrantModel
    {
        public int Id { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public DateTime RegisteredAt { get; set; }
    }

    public class EventRegistrationsPageModel
    {
        public int EventId { get; set; }
        public string? EventTitle { get; set; }
        public List<EventRegistrantModel> Registrations { get; set; } = new();
    }
}
