using KMC.EventClient.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace KMC.EventClient.Services
{
    public class ApiService(
        HttpClient httpClient,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ApiService> logger) : IApiService
    {
        private readonly HttpClient _httpClient = httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
        private readonly ILogger<ApiService> _logger = logger;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private static readonly JsonSerializerOptions _jsonOptionsCamelCase = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        private string? GetAuthToken()
        {
            try
            {
                return _httpContextAccessor.HttpContext?.Session.GetString("JWTToken");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting auth token");
                return null;
            }
        }

        private HttpRequestMessage CreateRequest(HttpMethod method, string url)
        {
            var request = new HttpRequestMessage(method, url);
            var token = GetAuthToken();
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return request;
        }

        public async Task<LoginResponse?> LoginAsync(string username, string password)
        {
            try
            {
                Console.WriteLine($"========== LOGIN ATTEMPT ==========");
                Console.WriteLine($"Username: {username}");
                Console.WriteLine($"API URL: {_httpClient.BaseAddress}api/auth/login");
                Console.WriteLine($"===================================");

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                    return null;

                var payload = new { username, password };
                var json = JsonSerializer.Serialize(payload, _jsonOptionsCamelCase);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("api/auth/login", content);
                var responseText = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"Status: {response.StatusCode}");
                Console.WriteLine($"Response: {responseText}");

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Login failed - Status: {response.StatusCode}");
                    return null;
                }

                var result = JsonSerializer.Deserialize<JsonElement>(responseText);

                var token = result.TryGetProperty("token", out var tokenProp)
                    ? tokenProp.GetString() ?? string.Empty
                    : string.Empty;

                var user = result.TryGetProperty("username", out var userProp)
                    ? userProp.GetString() ?? string.Empty
                    : string.Empty;

                var role = result.TryGetProperty("role", out var roleProp)
                    ? roleProp.GetString() ?? "User"
                    : "User";

                if (string.IsNullOrEmpty(token))
                {
                    Console.WriteLine("No token in response");
                    return null;
                }

                Console.WriteLine($"Login successful! Token length: {token.Length}");

                return new LoginResponse
                {
                    Token = token,
                    Username = user,
                    Role = role
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login Exception: {ex.Message}");
                return null;
            }
        }

        public async Task<(bool Succeeded, string? ErrorMessage)> RegisterAsync(RegisterModel model)
        {
            try
            {
                if (model == null)
                    return (false, "Invalid registration request data.");

                var payload = new
                {
                    username = model.Username?.Trim() ?? string.Empty,
                    email = model.Email?.Trim().ToLower() ?? string.Empty,
                    password = model.Password ?? string.Empty,
                    fullName = model.FullName?.Trim() ?? string.Empty,
                    role = model.Role ?? "User"
                };

                var json = JsonSerializer.Serialize(payload, _jsonOptionsCamelCase);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("POST api/auth/register for user: {Username}", model.Username);
                Console.WriteLine($"Request JSON: {json}");

                var response = await _httpClient.PostAsync("api/auth/register", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"Status: {response.StatusCode}");
                Console.WriteLine($"Response: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Registration successful!");
                    return (true, null);
                }

                _logger.LogError("Registration failed: {StatusCode} - {Error}", response.StatusCode, responseContent);

                string? errorMessage = null;
                try
                {
                    var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    if (result.TryGetProperty("message", out var messageProp))
                    {
                        errorMessage = messageProp.GetString();
                    }
                }
                catch
                {
                 
                }

                if (string.IsNullOrEmpty(errorMessage))
                {
                    errorMessage = $"Registration failed. Status: {response.StatusCode}";
                }

                return (false, errorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RegisterAsync Exception");
                return (false, ex.Message);
            }
        }

        public async Task<List<EventModel>> GetPublicEventsAsync(DateTime? date, string? type, string? keyword = null)
        {
            try
            {
                var url = "api/events/public?";
                if (date.HasValue)
                    url += $"date={date.Value:yyyy-MM-dd}&";
                if (!string.IsNullOrEmpty(type))
                    url += $"eventType={type}&";
                if (!string.IsNullOrEmpty(keyword))
                    url += $"keyword={Uri.EscapeDataString(keyword)}";

                _logger.LogInformation("GET {Url}", url);
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var events = JsonSerializer.Deserialize<List<EventModel>>(json, _jsonOptions) ?? [];
                    return events;
                }

                return [];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetPublicEventsAsync");
                return [];
            }
        }

        public async Task<List<EventModel>> GetMyEventsAsync()
        {
            try
            {
                var request = CreateRequest(HttpMethod.Get, "api/events");
                var response = await _httpClient.SendAsync(request);
                var responseText = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var events = JsonSerializer.Deserialize<List<EventModel>>(responseText, _jsonOptions) ?? [];
                    return events;
                }

                return [];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetMyEventsAsync");
                return [];
            }
        }

        public async Task<EventModel?> GetEventByIdAsync(int id)
        {
            try
            {
                var request = CreateRequest(HttpMethod.Get, $"api/events/{id}");
                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var eventItem = JsonSerializer.Deserialize<EventModel>(json, _jsonOptions);
                    return eventItem;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetEventByIdAsync");
                return null;
            }
        }

        public async Task<bool> CreateEventAsync(EventModel newEvent)
        {
            try
            {
                if (newEvent.Date == default)
                    return false;

                var payload = new
                {
                    title = newEvent.Title?.Trim() ?? "",
                    description = newEvent.Description?.Trim() ?? "",
                    date = newEvent.Date.ToString("yyyy-MM-ddTHH:mm:ss"),
                    location = newEvent.Location?.Trim() ?? "",
                    eventType = newEvent.EventType?.Trim() ?? "",
                    maxParticipants = newEvent.MaxParticipants > 0 ? newEvent.MaxParticipants : 50
                };

                var json = JsonSerializer.Serialize(payload, _jsonOptionsCamelCase);
                var request = CreateRequest(HttpMethod.Post, "api/events");
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                    return true;

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateEventAsync");
                return false;
            }
        }

        public async Task<bool> UpdateEventAsync(int id, EventModel updatedEvent)
        {
            try
            {
                var payload = new
                {
                    id = id,
                    title = updatedEvent.Title?.Trim(),
                    description = updatedEvent.Description?.Trim(),
                    date = updatedEvent.Date.ToString("yyyy-MM-ddTHH:mm:ss"),
                    location = updatedEvent.Location?.Trim(),
                    eventType = updatedEvent.EventType?.Trim(),
                    maxParticipants = updatedEvent.MaxParticipants
                };

                var json = JsonSerializer.Serialize(payload);
                var request = CreateRequest(HttpMethod.Put, $"api/events/{id}");
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateEventAsync");
                return false;
            }
        }

        public async Task<bool> DeleteEventAsync(int id)
        {
            try
            {
                var request = CreateRequest(HttpMethod.Delete, $"api/events/{id}");
                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeleteEventAsync");
                return false;
            }
        }

        public async Task<(bool Succeeded, string? ErrorMessage)> RegisterForEventAsync(EventRegistrationModel model)
        {
            try
            {
                var payload = new
                {
                    eventId = model.EventId,
                    fullName = model.FullName?.Trim(),
                    email = model.Email?.Trim(),
                    phoneNumber = model.PhoneNumber?.Trim(),
                    notes = model.Notes?.Trim()
                };

                var json = JsonSerializer.Serialize(payload);
                var request = CreateRequest(HttpMethod.Post, $"api/events/{model.EventId}/register");
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return (true, null);
                }

                string? errorMessage = null;
                try
                {
                    var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    if (result.ValueKind == JsonValueKind.String)
                    {
                        errorMessage = result.GetString();
                    }
                    else if (result.ValueKind == JsonValueKind.Object && result.TryGetProperty("message", out var messageProp))
                    {
                        errorMessage = messageProp.GetString();
                    }
                }
                catch { }

                if (string.IsNullOrEmpty(errorMessage))
                {
                    errorMessage = $"Registration failed. Status: {response.StatusCode}";
                }

                return (false, errorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RegisterForEventAsync");
                return (false, "Registration failed. Please try again.");
            }
        }

        public async Task<(bool Succeeded, string? ErrorMessage)> ForgotPasswordAsync(string email, string newPassword)
        {
            try
            {
                var payload = new
                {
                    email = email.Trim().ToLower(),
                    newPassword = newPassword
                };

                var json = JsonSerializer.Serialize(payload, _jsonOptionsCamelCase);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("api/auth/forgot-password", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return (true, null);
                }

                string? errorMessage = null;
                try
                {
                    var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    if (result.TryGetProperty("message", out var messageProp))
                    {
                        errorMessage = messageProp.GetString();
                    }
                }
                catch { }

                if (string.IsNullOrEmpty(errorMessage))
                {
                    errorMessage = $"Password reset failed. Status: {response.StatusCode}";
                }

                return (false, errorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ForgotPasswordAsync Exception");
                return (false, ex.Message);
            }
        }

        public async Task<List<EventModel>> GetAllEventsAsync()
        {
            try
            {
                var request = CreateRequest(HttpMethod.Get, "api/events/all");
                var response = await _httpClient.SendAsync(request);
                var responseText = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<List<EventModel>>(responseText, _jsonOptions) ?? [];
                }

                return [];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAllEventsAsync");
                return [];
            }
        }

        public async Task<bool> ApproveEventAsync(int id)
        {
            try
            {
                var request = CreateRequest(HttpMethod.Put, $"api/events/{id}/approve");
                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ApproveEventAsync");
                return false;
            }
        }

        public async Task<List<EventModel>> GetMyRegistrationsAsync()
        {
            try
            {
                var request = CreateRequest(HttpMethod.Get, "api/events/my-registrations");
                var response = await _httpClient.SendAsync(request);
                var responseText = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("GetMyRegistrationsAsync failed. Status: {Status}. Body: {Body}", response.StatusCode, responseText);
                    return [];
                }

                var registrations = JsonSerializer.Deserialize<List<JsonElement>>(responseText, _jsonOptions) ?? [];
                var events = new List<EventModel>();

                foreach (var reg in registrations)
                {
                    try
                    {
                        var e = new EventModel
                        {
                            Id = TryGetInt(reg, "eventId"),
                            Title = TryGetString(reg, "eventTitle"),
                            Description = TryGetString(reg, "eventDescription"),
                            Location = TryGetString(reg, "eventLocation"),
                            Date = TryGetDate(reg, "eventDate"),
                            OrganizerName = TryGetString(reg, "organizerName")
                        };
                        events.Add(e);
                    }
                    catch (Exception itemEx)
                    {

                        _logger.LogError(itemEx, "GetMyRegistrationsAsync - skipped one malformed row: {Row}", reg.ToString());
                    }
                }

                _logger.LogInformation("GetMyRegistrationsAsync returned {Count} registration(s).", events.Count);
                return events;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetMyRegistrationsAsync");
                return [];
            }
        }

        public async Task<EventRegistrationsPageModel?> GetEventRegistrationsAsync(int eventId)
        {
            try
            {
                var request = CreateRequest(HttpMethod.Get, $"api/events/{eventId}/registrations");
                var response = await _httpClient.SendAsync(request);
                var responseText = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("GetEventRegistrationsAsync failed. Status: {Status}. Body: {Body}", response.StatusCode, responseText);
                    return null;
                }

                var root = JsonSerializer.Deserialize<JsonElement>(responseText, _jsonOptions);

                var page = new EventRegistrationsPageModel
                {
                    EventId = TryGetInt(root, "eventId"),
                    EventTitle = TryGetString(root, "eventTitle")
                };

                if (TryGetArray(root, "registrations", out var regArray))
                {
                    foreach (var reg in regArray)
                    {
                        page.Registrations.Add(new EventRegistrantModel
                        {
                            Id = TryGetInt(reg, "id"),
                            FullName = TryGetString(reg, "fullName"),
                            Email = TryGetString(reg, "email"),
                            PhoneNumber = TryGetString(reg, "phoneNumber"),
                            RegisteredAt = TryGetDate(reg, "registeredAt")
                        });
                    }
                }

                return page;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetEventRegistrationsAsync");
                return null;
            }
        }

        private static bool TryGetArray(JsonElement el, string propertyName, out JsonElement.ArrayEnumerator array)
        {
            foreach (var prop in el.EnumerateObject())
            {
                if (string.Equals(prop.Name, propertyName, StringComparison.OrdinalIgnoreCase) && prop.Value.ValueKind == JsonValueKind.Array)
                {
                    array = prop.Value.EnumerateArray();
                    return true;
                }
            }
            array = default;
            return false;
        }

        private static string? TryGetString(JsonElement el, string propertyName)
        {
            foreach (var prop in el.EnumerateObject())
            {
                if (string.Equals(prop.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                    return prop.Value.ValueKind == JsonValueKind.Null ? null : prop.Value.GetString();
            }
            return null;
        }

        private static int TryGetInt(JsonElement el, string propertyName)
        {
            foreach (var prop in el.EnumerateObject())
            {
                if (string.Equals(prop.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                    return prop.Value.ValueKind == JsonValueKind.Number ? prop.Value.GetInt32() : 0;
            }
            return 0;
        }

        private static DateTime TryGetDate(JsonElement el, string propertyName)
        {
            foreach (var prop in el.EnumerateObject())
            {
                if (string.Equals(prop.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                    return prop.Value.ValueKind == JsonValueKind.Null ? default : prop.Value.GetDateTime();
            }
            return default;
        }

        public async Task<UserProfileModel?> GetProfileAsync()
        {
            try
            {
                var request = CreateRequest(HttpMethod.Get, "api/auth/profile");
                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<UserProfileModel>(json, _jsonOptions);
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetProfileAsync");
                return null;
            }
        }

        public async Task<(bool Succeeded, string? ErrorMessage)> UpdateProfileAsync(string fullName, string email, string password)
        {
            try
            {
                var payload = new
                {
                    fullName = fullName?.Trim(),
                    email = email?.Trim().ToLower(),
                    password = password
                };

                var json = JsonSerializer.Serialize(payload, _jsonOptionsCamelCase);
                var request = CreateRequest(HttpMethod.Put, "api/auth/update-profile");
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return (true, null);
                }

                string? errorMessage = null;
                try
                {
                    var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    if (result.TryGetProperty("message", out var messageProp))
                    {
                        errorMessage = messageProp.GetString();
                    }
                }
                catch { }

                return (false, errorMessage ?? "Failed to update profile.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateProfileAsync Exception");
                return (false, ex.Message);
            }
        }
    }
}