C:\Users\Admin\Desktop\KMC.EventPlatformAPI\Pages\Admin\Dashboard.cshtml.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using KMC.EventPlatformAPI.Data;
using KMC.EventPlatformAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace KMC.EventPlatformAPI.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class DashboardModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DashboardModel> _logger;

        public DashboardModel(ApplicationDbContext context, ILogger<DashboardModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public int TotalEvents { get; set; }
        public int PendingEvents { get; set; }
        public int AcceptedEvents { get; set; }
        public int RejectedEvents { get; set; }
        public List<Event> PendingEventsList { get; set; } = new();

        public async Task OnGetAsync()
        {
            try
            {
                TotalEvents = await _context.Events.CountAsync();
                PendingEvents = await _context.Events.CountAsync(e => e.Status == "Pending");
                AcceptedEvents = await _context.Events.CountAsync(e => e.Status == "Accepted");
                RejectedEvents = await _context.Events.CountAsync(e => e.Status == "Rejected");

                PendingEventsList = await _context.Events
                    .Where(e => e.Status == "Pending")
                    .OrderByDescending(e => e.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin dashboard");
                TempData["ErrorMessage"] = "Failed to load dashboard data.";
            }
        }

        public async Task<IActionResult> OnPostApproveAsync(int id)
        {
            try
            {
                var @event = await _context.Events.FindAsync(id);
                if (@event == null)
                {
                    return NotFound();
                }

                @event.Status = "Accepted";
                @event.UpdatedAt = DateTime.UtcNow;

                _context.Events.Update(@event);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Event '{@event.Name}' approved successfully!";
                _logger.LogInformation($"Event {id} approved by admin");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving event");
                TempData["ErrorMessage"] = "Failed to approve event.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRejectAsync(int id, string reason)
        {
            try
            {
                var @event = await _context.Events.FindAsync(id);
                if (@event == null)
                {
                    return NotFound();
                }

                @event.Status = "Rejected";
                @event.RejectionReason = reason;
                @event.UpdatedAt = DateTime.UtcNow;

                _context.Events.Update(@event);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Event '{@event.Name}' rejected.";
                _logger.LogInformation($"Event {id} rejected by admin. Reason: {reason}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting event");
                TempData["ErrorMessage"] = "Failed to reject event.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            try
            {
                var @event = await _context.Events.FindAsync(id);
                if (@event == null)
                {
                    return NotFound();
                }

                _context.Events.Remove(@event);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Event '{@event.Name}' deleted.";
                _logger.LogInformation($"Event {id} deleted by admin");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting event");
                TempData["ErrorMessage"] = "Failed to delete event.";
            }

            return RedirectToPage();
        }
    }
}