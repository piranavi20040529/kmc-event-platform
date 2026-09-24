C:\Users\Admin\Desktop\KMC.EventPlatformAPI\Pages\Events\List.cshtml.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using KMC.EventPlatformAPI.Data;
using KMC.EventPlatformAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace KMC.EventPlatformAPI.Pages.Events
{
    [Authorize]
    public class ListModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ListModel> _logger;

        public ListModel(ApplicationDbContext context, ILogger<ListModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; } = "";

        [BindProperty(SupportsGet = true)]
        public string SortBy { get; set; } = "date";

        public IList<Event> Events { get; set; } = new List<Event>();
        public string CurrentUserRole { get; set; }

        public async Task OnGetAsync()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                CurrentUserRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

                var query = _context.Events.AsQueryable();

                // Search
                if (!string.IsNullOrEmpty(SearchTerm))
                {
                    query = query.Where(e => e.Name.Contains(SearchTerm) || 
                                             e.Description.Contains(SearchTerm));
                }

                // Filter by user's events and accepted events
                if (CurrentUserRole != "Admin")
                {
                    query = query.Where(e => e.OrganizerId == userId || e.Status == "Accepted");
                }

                // Sorting
                query = SortBy switch
                {
                    "name" => query.OrderBy(e => e.Name),
                    "date" => query.OrderByDescending(e => e.EventDate),
                    "recent" => query.OrderByDescending(e => e.CreatedAt),
                    _ => query.OrderByDescending(e => e.EventDate)
                };

                Events = await query.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading events");
                TempData["ErrorMessage"] = "Failed to load events. Please try again.";
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            try
            {
                var @event = await _context.Events.FindAsync(id);
                if (@event == null)
                {
                    TempData["ErrorMessage"] = "Event not found.";
                    return RedirectToPage();
                }

                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var isAdmin = User.IsInRole("Admin");

                // Check authorization
                if (@event.OrganizerId != userId && !isAdmin)
                {
                    TempData["ErrorMessage"] = "You don't have permission to delete this event.";
                    return RedirectToPage();
                }

                _context.Events.Remove(@event);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Event '{@event.Name}' deleted successfully.";
                _logger.LogInformation($"Event {id} deleted by user {userId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting event");
                TempData["ErrorMessage"] = "Failed to delete event. Please try again.";
            }

            return RedirectToPage();
        }
    }
}