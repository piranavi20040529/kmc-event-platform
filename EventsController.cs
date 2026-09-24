using KMC.EventPlatformAPI.Data;
using KMC.EventPlatformAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace KMC.EventPlatformAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class EventsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public EventsController(ApplicationDbContext context)
        {
            _context = context;
        }

      
        [HttpGet("public")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPublicEvents(DateTime? date, string? eventType, string? keyword)
        {
            var query = _context.Events.AsQueryable();

            if (date.HasValue)
                query = query.Where(e => e.Date.Date == date.Value.Date);

            if (!string.IsNullOrEmpty(eventType))
            {
                var typeLower = eventType.Trim().ToLower();
                query = query.Where(e => e.EventType.ToLower() == typeLower);
            }

            if (!string.IsNullOrEmpty(keyword))
            {
                var kw = keyword.Trim().ToLower();
                query = query.Where(e => e.Title.ToLower().Contains(kw) || e.Description.ToLower().Contains(kw));
            }

            var events = await query
                .Include(e => e.CreatedByUser)
                .Select(e => new
                {
                    e.Id,
                    e.Title,
                    e.Description,
                    e.Date,
                    e.Location,
                    e.EventType,
                    e.MaxParticipants,
                    OrganizerName = e.CreatedByUser != null ? e.CreatedByUser.FullName : "Unknown",
                    ParticipantCount = _context.EventRegistrations.Count(r => r.EventId == e.Id)
                })
                .ToListAsync();

            return Ok(events);
        }

     
        [HttpGet]
        public async Task<IActionResult> GetMyEvents()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

          
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var events = await _context.Events
                .Where(e => e.CreatedByUserId == userId)
                .Include(e => e.CreatedByUser)
                .Select(e => new
                {
                    e.Id,
                    e.Title,
                    e.Description,
                    e.Date,
                    e.Location,
                    e.EventType,
                    e.MaxParticipants,
                    OrganizerName = e.CreatedByUser != null ? e.CreatedByUser.FullName : "Unknown",
                    ParticipantCount = _context.EventRegistrations.Count(r => r.EventId == e.Id)
                })
                .ToListAsync();

            return Ok(events);
        }

    
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetEvent(int id)
        {
            var eventItem = await _context.Events
                .Include(e => e.CreatedByUser)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (eventItem == null)
                return NotFound();

            var result = new
            {
                eventItem.Id,
                eventItem.Title,
                eventItem.Description,
                eventItem.Date,
                eventItem.Location,
                eventItem.EventType,
                eventItem.MaxParticipants,
                OrganizerName = eventItem.CreatedByUser?.FullName ?? "Unknown",
                ParticipantCount = await _context.EventRegistrations.CountAsync(r => r.EventId == id)
            };

            return Ok(result);
        }

      
        [HttpPost]
        public async Task<IActionResult> CreateEvent([FromBody] Event newEvent)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

           
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(newEvent.Title))
                return BadRequest("Title is required");

            if (string.IsNullOrWhiteSpace(newEvent.Description))
                return BadRequest("Description is required");

            if (newEvent.Date == default)
                return BadRequest("Date is required");

            newEvent.CreatedByUserId = userId;
            newEvent.CreatedAt = DateTime.UtcNow; 
            newEvent.IsApproved = true; 

            _context.Events.Add(newEvent);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEvent), new { id = newEvent.Id }, newEvent);
        }

    
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEvent(int id, [FromBody] Event updatedEvent)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

         
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var existingEvent = await _context.Events.FindAsync(id);
            if (existingEvent == null)
                return NotFound();

            if (existingEvent.CreatedByUserId != userId)
                return Forbid("You are not the creator of this event");

          
            existingEvent.Title = updatedEvent.Title;
            existingEvent.Description = updatedEvent.Description;
            existingEvent.Date = updatedEvent.Date;
            existingEvent.Location = updatedEvent.Location;
            existingEvent.EventType = updatedEvent.EventType;
            existingEvent.MaxParticipants = updatedEvent.MaxParticipants;

            await _context.SaveChangesAsync();
            return Ok(existingEvent);
        }

       
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

         
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var eventItem = await _context.Events.FindAsync(id);
            if (eventItem == null)
                return NotFound();

            if (eventItem.CreatedByUserId != userId)
                return Forbid("You are not the creator of this event");

            _context.Events.Remove(eventItem);
            await _context.SaveChangesAsync();

            return NoContent();
        }

    
        [HttpPost("{id}/register")]
        public async Task<IActionResult> RegisterForEvent(int id, [FromBody] EventRegistration registration)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);


            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var eventItem = await _context.Events.FindAsync(id);
            if (eventItem == null)
                return NotFound();

            
            var existingRegistration = await _context.EventRegistrations
                .FirstOrDefaultAsync(r => r.EventId == id && r.UserId == userId);

            if (existingRegistration != null)
                return BadRequest("Already registered for this event");

            var currentCount = await _context.EventRegistrations.CountAsync(r => r.EventId == id);
            if (eventItem.MaxParticipants > 0 && currentCount >= eventItem.MaxParticipants)
                return BadRequest("This event is already full.");

            registration.UserId = userId;
            registration.EventId = id;
            registration.RegisteredAt = DateTime.UtcNow;

            _context.EventRegistrations.Add(registration);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Registration successful" });
        }

      
        [HttpGet("{id}/registrations")]
        public async Task<IActionResult> GetEventRegistrations(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var eventItem = await _context.Events.FindAsync(id);
            if (eventItem == null)
                return NotFound();

            if (eventItem.CreatedByUserId != userId)
                return Forbid("You are not authorized to view registrations for this event");

            var registrations = await _context.EventRegistrations
                .Where(r => r.EventId == id)
                .OrderBy(r => r.RegisteredAt)
                .Select(r => new
                {
                    r.Id,
                    r.FullName,
                    r.Email,
                    r.PhoneNumber,
                    r.RegisteredAt
                })
                .ToListAsync();

            return Ok(new
            {
                EventId = eventItem.Id,
                EventTitle = eventItem.Title,
                Registrations = registrations
            });
        }

      
        [HttpGet("my-registrations")]
        public async Task<IActionResult> GetMyRegistrations()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var registrations = await _context.EventRegistrations
                .Where(r => r.UserId == userId && r.Event != null)
                .Include(r => r.Event)
                .ThenInclude(e => e!.CreatedByUser)
                .Select(r => new
                {
                    r.Id,
                    r.EventId,
                    r.RegisteredAt,
                    EventTitle = r.Event!.Title,
                    EventDate = r.Event.Date,
                    EventLocation = r.Event.Location,
                    EventDescription = r.Event.Description,
                    OrganizerName = r.Event.CreatedByUser != null ? r.Event.CreatedByUser.FullName : "Unknown"
                })
                .ToListAsync();

            return Ok(registrations);
        }
    }
}