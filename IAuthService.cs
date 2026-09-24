using KMC.EventPlatformAPI.Models;

namespace KMC.EventPlatformAPI.Services
{
    public interface IAuthService
    {
        Task<(string? Token, string? Role)> LoginAsync(string username, string password);
        Task<(bool Succeeded, IEnumerable<string> Errors)> RegisterUserAsync(User user, string password);
    }
}
