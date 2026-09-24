using KMC.EventClient.Models;
using KMC.EventClient.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace KMC.EventClient.Controllers
{
    public class EventsController : Controller
    {
        private readonly IApiService _apiService;
        private readonly IMemoryCache _cache;

        public EventsController(IApiService apiService, IMemoryCache memoryCache)
        {
            _apiService = apiService;
            _cache = memoryCache;
        }

        private void SetUserViewBag()
        {
            ViewBag.Username = HttpContext.Session.GetString("Username") ?? "Guest";
            ViewBag.IsLoggedIn = !string.IsNullOrEmpty(HttpContext.Session.GetString("JWTToken"));
            ViewBag.Role = HttpContext.Session.GetString("Role") ?? "User";
        }

        private bool IsUserLoggedIn()
        {
            return !string.IsNullOrEmpty(HttpContext.Session.GetString("JWTToken"));
        }

        public async Task<IActionResult> Index(DateTime? date, string? type, string? keyword)
        {
            SetUserViewBag();
            try
            {
                if (IsUserLoggedIn())
                {
                    var role = HttpContext.Session.GetString("Role");

                    if (role == "Admin")
                        return RedirectToAction(nameof(AdminDashboard));

                    if (role == "Organizer")
                        return RedirectToAction(nameof(MyEvents));

                    return RedirectToAction(nameof(PublicEvents));
                }

                var events = await _apiService.GetPublicEventsAsync(date, type, keyword);
                return View(events ?? new List<EventModel>());
            }
            catch
            {
                return View(new List<EventModel>());
            }
        }

        public async Task<IActionResult> PublicEvents(DateTime? date, string? type, string? keyword)
        {
            SetUserViewBag();
            if (!IsUserLoggedIn()) return RedirectToAction(nameof(Index));

            var role = HttpContext.Session.GetString("Role");
            if (role == "Admin") return RedirectToAction(nameof(AdminDashboard));

            try
            {
                var events = await _apiService.GetPublicEventsAsync(date, type, keyword);
                return View(events ?? new List<EventModel>());
            }
            catch
            {
                return View(new List<EventModel>());
            }
        }

        public async Task<IActionResult> MyEvents()
        {
            SetUserViewBag();
            if (!IsUserLoggedIn()) return RedirectToAction("Login", "Auth");

            try
            {
                var events = await _apiService.GetMyEventsAsync();
                return View(events ?? new List<EventModel>());
            }
            catch
            {
                return View(new List<EventModel>());
            }
        }

        [HttpGet]
        public IActionResult Create()
        {
            SetUserViewBag();
            if (!IsUserLoggedIn()) return RedirectToAction("Login", "Auth");
            return View(new EventModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EventModel newEvent)
        {
            SetUserViewBag();
            if (!IsUserLoggedIn()) return RedirectToAction("Login", "Auth");

            if (!ModelState.IsValid) return View(newEvent);

            var success = await _apiService.CreateEventAsync(newEvent);
            if (success)
            {
                TempData["SuccessMessage"] = "Event created successfully!";
                return RedirectToAction(nameof(MyEvents));
            }

            TempData["ErrorMessage"] = "Failed to create event.";
            return View(newEvent);
        }

        [HttpGet]
        public async Task<IActionResult> ViewRegistrations(int id)
        {
            SetUserViewBag();
            if (!IsUserLoggedIn()) return RedirectToAction("Login", "Auth");

            var page = await _apiService.GetEventRegistrationsAsync(id);
            if (page == null)
            {
                TempData["ErrorMessage"] = "Could not load registrations for this event.";
                return RedirectToAction(nameof(MyEvents));
            }

            return View(page);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            SetUserViewBag();
            if (!IsUserLoggedIn()) return RedirectToAction("Login", "Auth");

            var eventItem = await _apiService.GetEventByIdAsync(id);
            if (eventItem == null) return RedirectToAction(nameof(MyEvents));
            return View(eventItem);
        }

        [HttpGet]
        public async Task<IActionResult> Register(int id)
        {
            SetUserViewBag();
            if (!IsUserLoggedIn()) return RedirectToAction("Login", "Auth");

            var eventItem = await _apiService.GetEventByIdAsync(id);
            if (eventItem == null) return RedirectToAction(nameof(PublicEvents));

            if (eventItem.MaxParticipants > 0 && eventItem.ParticipantCount >= eventItem.MaxParticipants)
            {
                TempData["ErrorMessage"] = "This event is already full.";
                return RedirectToAction(nameof(PublicEvents));
            }

           
            ViewBag.EventTitle = eventItem.Title;
            ViewBag.EventLocation = eventItem.Location;
            ViewBag.EventDate = eventItem.Date.ToString("dd MMM yyyy, hh:mm tt");
            ViewBag.EventOrganizer = eventItem.OrganizerName;
            ViewBag.EventType = eventItem.EventType;
            ViewBag.EventParticipants = $"{eventItem.ParticipantCount} / {eventItem.MaxParticipants}";

            return View(new EventRegistrationModel { EventId = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(int id, EventRegistrationModel model)
        {
            SetUserViewBag();
            if (!IsUserLoggedIn()) return RedirectToAction("Login", "Auth");

            if (!ModelState.IsValid) return View(model);

            model.EventId = id;
            var (success, errorMessage) = await _apiService.RegisterForEventAsync(model);

            TempData[success ? "SuccessMessage" : "ErrorMessage"] =
                success ? "You have successfully registered for the event!" : (errorMessage ?? "Registration failed.");
            return RedirectToAction(nameof(PublicEvents));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EventModel model)
        {
            SetUserViewBag();
            if (!IsUserLoggedIn()) return RedirectToAction("Login", "Auth");

            if (!ModelState.IsValid) return View(model);

            var success = await _apiService.UpdateEventAsync(id, model);
            if (success)
            {
                TempData["SuccessMessage"] = "Event updated successfully!";
                return RedirectToAction(nameof(MyEvents));
            }

            TempData["ErrorMessage"] = "Failed to update event.";
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, string returnUrl = null)
        {
            SetUserViewBag();
            if (!IsUserLoggedIn()) return RedirectToAction("Login", "Auth");

            var success = await _apiService.DeleteEventAsync(id);
            if (success)
            {
                TempData["SuccessMessage"] = "Event deleted successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to delete event.";
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            var role = HttpContext.Session.GetString("Role");
            if (role == "Admin")
            {
                return RedirectToAction(nameof(AdminDashboard));
            }
            return RedirectToAction(nameof(MyEvents));
        }

        [HttpGet]
        public async Task<IActionResult> AdminDashboard()
        {
            SetUserViewBag();
            if (!IsUserLoggedIn()) return RedirectToAction("Login", "Auth");

            var role = HttpContext.Session.GetString("Role");
            if (role != "Admin")
            {
                return RedirectToAction(nameof(MyEvents));
            }

            var events = await _apiService.GetAllEventsAsync();
            var profile = await _apiService.GetProfileAsync();
            ViewBag.Profile = profile;

            return View(events ?? new List<EventModel>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(string fullName, string email, string password)
        {
            SetUserViewBag();
            if (!IsUserLoggedIn()) return RedirectToAction("Login", "Auth");

            var role = HttpContext.Session.GetString("Role");
            if (role != "Admin")
            {
                TempData["ErrorMessage"] = "Access denied.";
                return RedirectToAction(nameof(Index));
            }

            var (success, errorMessage) = await _apiService.UpdateProfileAsync(fullName, email, password);
            if (success)
            {
                TempData["SuccessMessage"] = "Profile details updated successfully!";
                if (!string.IsNullOrEmpty(fullName))
                {
                    HttpContext.Session.SetString("Username", fullName);
                }
            }
            else
            {
                TempData["ErrorMessage"] = errorMessage ?? "Failed to update profile details.";
            }

            return RedirectToAction(nameof(AdminDashboard));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            SetUserViewBag();
            if (!IsUserLoggedIn()) return RedirectToAction("Login", "Auth");

            var role = HttpContext.Session.GetString("Role");
            if (role != "Admin")
            {
                TempData["ErrorMessage"] = "Access denied. Admin role required.";
                return RedirectToAction(nameof(Index));
            }

            var success = await _apiService.ApproveEventAsync(id);
            if (success)
            {
                TempData["SuccessMessage"] = "Event approved successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to approve event.";
            }

            return RedirectToAction(nameof(AdminDashboard));
        }

        [HttpGet]
        public async Task<IActionResult> MyRegistrations()
        {
            SetUserViewBag();
            if (!IsUserLoggedIn()) return RedirectToAction("Login", "Auth");

            var events = await _apiService.GetMyRegistrationsAsync();
            return View(events ?? new List<EventModel>());
        }
    }
}