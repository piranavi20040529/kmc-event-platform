using KMC.EventClient.Models;
using KMC.EventClient.Services;
using Microsoft.AspNetCore.Mvc;

namespace KMC.EventClient.Controllers
{
    public class AuthController : Controller
    {
        private readonly IApiService _apiService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IApiService apiService, ILogger<AuthController> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                if (string.IsNullOrEmpty(model.Username) || string.IsNullOrEmpty(model.Password))
                {
                    TempData["ErrorMessage"] = "Username and password are required.";
                    return View(model);
                }

                _logger.LogInformation($" Attempting login for: {model.Username}");

                var response = await _apiService.LoginAsync(model.Username, model.Password);

                if (response == null || string.IsNullOrEmpty(response.Token))
                {
                    _logger.LogWarning($" Login failed for: {model.Username}");
                    TempData["ErrorMessage"] = "Invalid username or password. Please try again.";
                    return View(model);
                }

                // Store in session
                HttpContext.Session.SetString("JWTToken", response.Token);
                HttpContext.Session.SetString("Username", response.Username);
                HttpContext.Session.SetString("Role", response.Role ?? "Public");

                _logger.LogInformation($" Login successful! User: {response.Username}, Role: {response.Role}");

                TempData["SuccessMessage"] = $"Welcome {response.Username}!";
                return RedirectToAction("Index", "Events");
            }
            catch (Exception ex)
            {
                _logger.LogError($" Login error: {ex.Message}");
                TempData["ErrorMessage"] = $"Login error: {ex.Message}";
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterModel { Role = "Public" });
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                TempData["ErrorMessage"] = $"Validation failed: {errors}";
                return View(model);
            }

            try
            {
                if (string.IsNullOrEmpty(model.Role))
                    model.Role = "Public";

                // Trim all inputs
                model.Username = model.Username?.Trim() ?? string.Empty;
                model.Email = model.Email?.Trim().ToLower() ?? string.Empty;
                model.FullName = model.FullName?.Trim() ?? string.Empty;

                Console.WriteLine($" Sending registration request:");
                Console.WriteLine($"  Username: {model.Username}");
                Console.WriteLine($"  Email: {model.Email}");
                Console.WriteLine($"  FullName: {model.FullName}");
                Console.WriteLine($"  Role: {model.Role}");

                var (success, errorMessage) = await _apiService.RegisterAsync(model);

                if (success)
                {
                    TempData["SuccessMessage"] = "Registration successful! Please login.";
                    return RedirectToAction("Login");
                }

                TempData["ErrorMessage"] = errorMessage ?? "Registration failed. Username or email may already exist.";
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError($" Registration error: {ex.Message}");
                TempData["ErrorMessage"] = $"Registration error: {ex.Message}";
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var (success, errorMessage) = await _apiService.ForgotPasswordAsync(model.Email, model.NewPassword);

            if (success)
            {
                TempData["SuccessMessage"] = "Your password has been reset successfully! Please login with your new password.";
                return RedirectToAction("Login");
            }

            TempData["ErrorMessage"] = errorMessage ?? "Failed to reset password. Please ensure your email is correct.";
            return View(model);
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["SuccessMessage"] = "You have been logged out.";
            return RedirectToAction("Login");
        }
    }
}