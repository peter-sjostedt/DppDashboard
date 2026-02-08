using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DppDashboard.Services
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? Error { get; set; }
    }

    public class ApiClient
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly HttpClient _http;
        private string? _adminKey;

        public bool IsAuthenticated { get; private set; }

        public ApiClient()
        {
            _http = new HttpClient
            {
                BaseAddress = new Uri("https://dpp.petersjostedt.se")
            };
            _http.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        public async Task<bool> LoginAsync(string adminKey)
        {
            _adminKey = adminKey;
            _http.DefaultRequestHeaders.Remove("X-Admin-Key");
            _http.DefaultRequestHeaders.Add("X-Admin-Key", adminKey);

            try
            {
                var response = await _http.GetAsync("/api/admin/stats");
                if (response.IsSuccessStatusCode)
                {
                    IsAuthenticated = true;
                    return true;
                }
            }
            catch { }

            _adminKey = null;
            _http.DefaultRequestHeaders.Remove("X-Admin-Key");
            IsAuthenticated = false;
            return false;
        }

        public void Logout()
        {
            _adminKey = null;
            _http.DefaultRequestHeaders.Remove("X-Admin-Key");
            IsAuthenticated = false;
        }

        public async Task<ApiResponse<T>> GetAsync<T>(string endpoint)
        {
            try
            {
                var response = await _http.GetAsync(endpoint);
                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return new ApiResponse<T> { Success = false, Error = $"HTTP {(int)response.StatusCode}: {json}" };

                var data = JsonSerializer.Deserialize<T>(json, JsonOptions);
                return new ApiResponse<T> { Success = true, Data = data };
            }
            catch (Exception ex)
            {
                return new ApiResponse<T> { Success = false, Error = ex.Message };
            }
        }

        public async Task<string?> GetRawAsync(string endpoint)
        {
            try
            {
                var response = await _http.GetAsync(endpoint);
                return await response.Content.ReadAsStringAsync();
            }
            catch
            {
                return null;
            }
        }

        public async Task<string?> GetWithTenantKeyAsync(string endpoint, string tenantApiKey)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
                request.Headers.Add("X-API-Key", tenantApiKey);
                var response = await _http.SendAsync(request);
                return await response.Content.ReadAsStringAsync();
            }
            catch
            {
                return null;
            }
        }

        public async Task<string?> PostAsync(string endpoint, object data)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Headers.Add("X-Admin-Key", _adminKey);
                request.Content = new StringContent(
                    JsonSerializer.Serialize(data, JsonOptions),
                    Encoding.UTF8, "application/json");
                var response = await _http.SendAsync(request);
                return await response.Content.ReadAsStringAsync();
            }
            catch
            {
                return null;
            }
        }

        public async Task<string?> PutAsync(string endpoint, object data)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Put, endpoint);
                request.Headers.Add("X-Admin-Key", _adminKey);
                request.Content = new StringContent(
                    JsonSerializer.Serialize(data, JsonOptions),
                    Encoding.UTF8, "application/json");
                var response = await _http.SendAsync(request);
                return await response.Content.ReadAsStringAsync();
            }
            catch
            {
                return null;
            }
        }

        public async Task<string?> DeleteAsync(string endpoint)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Delete, endpoint);
                request.Headers.Add("X-Admin-Key", _adminKey);
                var response = await _http.SendAsync(request);
                return await response.Content.ReadAsStringAsync();
            }
            catch
            {
                return null;
            }
        }
    }
}
