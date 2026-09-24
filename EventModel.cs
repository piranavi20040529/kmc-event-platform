using System;
using System.ComponentModel.DataAnnotations;

namespace KMC.EventClient.Models
{
    public class EventModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        public string? Title { get; set; }

        [Required(ErrorMessage = "Description is required")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Date is required")]
        [DataType(DataType.DateTime)]
        public DateTime Date { get; set; }

        [Required(ErrorMessage = "Location is required")]
        public string? Location { get; set; }

        [Required(ErrorMessage = "Event Type is required")]
        public string? EventType { get; set; }

        [Required(ErrorMessage = "Max Participants is required")]
        [Range(1, 10000, ErrorMessage = "Max participants must be between 1 and 10000")]
        public int MaxParticipants { get; set; }

        public string? OrganizerName { get; set; }
        public int ParticipantCount { get; set; }
        public bool IsApproved { get; set; }
    }
}