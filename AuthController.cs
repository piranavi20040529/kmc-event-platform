using KMC.EventPlatformAPI.Models;
using KMC.EventPlatformAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace KMC.EventPlatformAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly UserManager<User> _userManager;

        public AuthController(IAuthService authService, UserManager<User> userManager)
        {
            _authService = authService;
            _userManager = userManager;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginRequest)
        {
            try
            {
                Console.WriteLine($" Login attempt for: {loginRequest?.Username}");

                if (loginRequest == null || string.IsNullOrEmpty(loginRequest.Username))
                    return BadRequest(new { message = "Username is required." });

                var (token, role) = await _authService.LoginAsync(loginRequest.Username, loginRequest.Password);

                if (string.IsNullOrEmpty(token))
                {
                    Console.WriteLine($" Login failed for: {loginRequest.Username}");
                    return Unauthorized(new { message = "Invalid username or password." });
                }

                Console.WriteLine($" Login successful: {loginRequest.Username}");
                return Ok(new
                {
                    token = token,
                    role = role ?? "Public",
                    username = loginRequest.Username
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Login error: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerRequest)
        {
            try
            {
                Console.WriteLine($" Registration request for: {registerRequest?.Username}");

                if (registerRequest == null)
                    return BadRequest(new { message = "Invalid request data." });

                if (string.IsNullOrWhiteSpace(registerRequest.Username))
                    return BadRequest(new { message = "Username is required." });

                if (string.IsNullOrWhiteSpace(registerRequest.Password))
                    return BadRequest(new { message = "Password is required." });

                if (registerRequest.Password.Length < 6)
                    return BadRequest(new { message = "Password must be at least 6 characters." });

                var user = new User
                {
                    UserName = registerRequest.Username.Trim(),
                    Email = registerRequest.Email?.Trim() ?? $"user_{Guid.NewGuid().ToString().Substring(0, 8)}@placeholder.local",
                    FullName = registerRequest.FullName?.Trim() ?? string.Empty,
                 
                    Role = registerRequest.Role ?? "Public",
                    CreatedAt = DateTime.UtcNow
                };

                var (succeeded, errors) = await _authService.RegisterUserAsync(user, registerRequest.Password);

                if (!succeeded)
                {
                    Console.WriteLine($" Registration failed for: {registerRequest.Username}");
                    return BadRequest(new { message = string.Join("; ", errors) });
                }

                Console.WriteLine($" Registration successful: {registerRequest.Username}");
                return Ok(new
                {
                    message = "User created successfully.",
                    role = user.Role
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Registration error: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Email))
                    return BadRequest(new { message = "Email is required." });

                if (string.IsNullOrWhiteSpace(request.NewPassword))
                    return BadRequest(new { message = "New Password is required." });

                if (request.NewPassword.Length < 6)
                    return BadRequest(new { message = "Password must be at least 6 characters." });

                var user = await _userManager.FindByEmailAsync(request.Email.Trim());
                if (user == null)
                {
                    return NotFound(new { message = "No user found with this email address." });
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);

                if (!result.Succeeded)
                {
                    return BadRequest(new { message = string.Join("; ", result.Errors.Select(e => e.Description)) });
                }

                Console.WriteLine($" Password reset successful for user: {user.UserName} (Email: {request.Email})");
                return Ok(new { message = "Password reset successfully." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($" ForgotPassword error: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound(new { message = "User not found." });

            return Ok(new
            {
                user.FullName,
                user.Email,
                user.UserName
            });
        }

        [HttpPut("update-profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto request)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return NotFound(new { message = "User not found." });

                if (!string.IsNullOrWhiteSpace(request.FullName))
                    user.FullName = request.FullName.Trim();

                if (!string.IsNullOrWhiteSpace(request.Email))
                    user.Email = request.Email.Trim().ToLower();

                if (!string.IsNullOrWhiteSpace(request.Password))
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var result = await _userManager.ResetPasswordAsync(user, token, request.Password);
                    if (!result.Succeeded)
                    {
                        return BadRequest(new { message = string.Join("; ", result.Errors.Select(e => e.Description)) });
                    }
                }

                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    return BadRequest(new { message = string.Join("; ", updateResult.Errors.Select(e => e.Description)) });
                }

                return Ok(new { message = "Profile updated successfully.", fullName = user.FullName, email = user.Email });
            }
            catch (Exception ex)
            {
                Console.WriteLine($" UpdateProfile error: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
