using KMC.EventPlatformAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace KMC.EventPlatformAPI.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;

        public AuthService(
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
        }

        public async Task<(string? Token, string? Role)> LoginAsync(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return (null, null);

            var user = await _userManager.FindByNameAsync(username);
            if (user == null)
            {
                Console.WriteLine($" User not found: {username}");
                return (null, null);
            }

            if (!await _userManager.CheckPasswordAsync(user, password))
            {
                Console.WriteLine($" Invalid password for: {username}");
                return (null, null);
            }

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Public";

            var token = GenerateJwtToken(user, role);
            Console.WriteLine($" Login successful: {username}");

            return (token, role);
        }

        public async Task<(bool Succeeded, IEnumerable<string> Errors)> RegisterUserAsync(User user, string password)
        {
            try
            {
                if (user == null || string.IsNullOrEmpty(password))
                    return (false, new[] { "Invalid registration data." });

                if (string.IsNullOrEmpty(user.UserName))
                    return (false, new[] { "Username is required." });

                if (string.IsNullOrEmpty(user.Role))
                {
                    user.Role = "Public";
                }
                else
                {
                    var roleLower = user.Role.Trim().ToLower();
                    if (roleLower == "organizer")
                        user.Role = "Organizer";
                    else
                        user.Role = "Public";
                }

                user.UserName = user.UserName.Trim().ToLower();

                if (string.IsNullOrEmpty(user.Email) || string.IsNullOrWhiteSpace(user.Email))
                {
                    user.Email = $"user_{Guid.NewGuid().ToString().Substring(0, 8)}@placeholder.local";
                }
                else
                {
                    user.Email = user.Email.Trim().ToLower();
                }

                user.NormalizedUserName = user.UserName.ToUpper();
                user.NormalizedEmail = user.Email.ToUpper();
                user.CreatedAt = DateTime.UtcNow;

                Console.WriteLine($" Creating user: {user.UserName} with email: {user.Email}");

                var existingUser = await _userManager.FindByNameAsync(user.UserName);
                if (existingUser != null)
                {
                    Console.WriteLine($" Username already exists: {user.UserName}");
                    return (false, new[] { "Username already exists." });
                }

                var result = await _userManager.CreateAsync(user, password);

                if (!result.Succeeded)
                {
                    Console.WriteLine($" User creation failed:");
                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine($"   - {error.Description}");
                    }
                    return (false, result.Errors.Select(e => e.Description).ToList());
                }

                await _userManager.AddToRoleAsync(user, user.Role);
                Console.WriteLine($" User created successfully: {user.UserName}");
                return (true, Enumerable.Empty<string>());
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Register error: {ex.Message}");
                return (false, new[] { ex.Message });
            }
        }

        private static string GenerateJwtToken(User user, string role)
        {
            if (user == null)
                return string.Empty;

            var jwtKey = "YourSuperSecretKeyHereWithAtLeast32CharactersLong";
            var jwtIssuer = "KMC.EventPlatformAPI";
            var jwtAudience = "KMC.EventClient";

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id ?? string.Empty),
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Role, role ?? "Public"),
                new Claim("FullName", user.FullName ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.Now.AddHours(24),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}