C:\Users\Admin\Desktop\KMC.EventPlatformAPI\Pages\Events\CreateEdit.cshtml.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using KMC.EventPlatformAPI.Data;
using KMC.EventPlatformAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace KMC.EventPlatformAPI.Pages.Events
{
    [Authorize]
    public class CreateEditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CreateEditModel> _logger;

        public CreateEditModel(ApplicationDbContext context, ILogger<CreateEditModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public Event Event { get; set; } = new();

        public bool IsEditMode { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id.HasValue)
            {
                Event = await _context.Events.FindAsync(id);
                if (Event == null)
                {
                    TempData["ErrorMessage"] = "Event not found.";
                    return RedirectToPage("/Events/List");
                }

                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (Event.OrganizerId != userId)
                {
                    TempData["ErrorMessage"] = "You don't have permission to edit this event.";
                    return RedirectToPage("/Events/List");
                }

                IsEditMode = true;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please fill in all required fields correctly.";
                return Page();
            }

            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                if (Event.Id == 0)
                {
                    // Create new event
                    Event.OrganizerId = userId;
                    Event.CreatedAt = DateTime.UtcNow;
                    Event.Status = "Approved";

                    _context.Events.Add(Event);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Event '{Event.Name}' created successfully!";
                    _logger.LogInformation($"Event created by user {userId}: {Event.Name}");
                }
                else
                {
                    // Edit existing event
                    var existingEvent = await _context.Events.FindAsync(Event.Id);
                    if (existingEvent == null)
                    {
                        TempData["ErrorMessage"] = "Event not found.";
                        return RedirectToPage("/Events/List");
                    }

                    if (existingEvent.OrganizerId != userId)
                    {
                        TempData["ErrorMessage"] = "You don't have permission to edit this event.";
                        return RedirectToPage("/Events/List");
                    }

                    existingEvent.Name = Event.Name;
                    existingEvent.Description = Event.Description;
                    existingEvent.EventDate = Event.EventDate;
                    existingEvent.Location = Event.Location;
                    existingEvent.Capacity = Event.Capacity;
                    existingEvent.UpdatedAt = DateTime.UtcNow;

                    _context.Events.Update(existingEvent);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Event '{Event.Name}' updated successfully!";
                    _logger.LogInformation($"Event {Event.Id} updated by user {userId}");
                }

                return RedirectToPage("/Events/List");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving event");
                TempData["ErrorMessage"] = "Failed to save event. Please try again.";
                return Page();
            }
        }
    }
}