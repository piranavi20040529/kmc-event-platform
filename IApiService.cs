using KMC.EventClient.Models;

namespace KMC.EventClient.Services
{
    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public interface IApiService
    {
        Task<List<EventModel>> GetPublicEventsAsync(DateTime? date, string? type, string? keyword = null);
        Task<List<EventModel>> GetMyEventsAsync();
        Task<EventModel?> GetEventByIdAsync(int id);
        Task<bool> CreateEventAsync(EventModel newEvent);
        Task<bool> UpdateEventAsync(int id, EventModel updatedEvent);
        Task<bool> DeleteEventAsync(int id);
        Task<(bool Succeeded, string? ErrorMessage)> RegisterForEventAsync(EventRegistrationModel model);

        Task<LoginResponse?> LoginAsync(string username, string password);
        Task<(bool Succeeded, string? ErrorMessage)> RegisterAsync(RegisterModel model);

        Task<(bool Succeeded, string? ErrorMessage)> ForgotPasswordAsync(string email, string newPassword);
        Task<List<EventModel>> GetAllEventsAsync();
        Task<bool> ApproveEventAsync(int id);
        Task<List<EventModel>> GetMyRegistrationsAsync();
        Task<EventRegistrationsPageModel?> GetEventRegistrationsAsync(int eventId);

        Task<UserProfileModel?> GetProfileAsync();
        Task<(bool Succeeded, string? ErrorMessage)> UpdateProfileAsync(string fullName, string email, string password);
    }
}