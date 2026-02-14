using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HospitexDPP.Services
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
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };

        private readonly HttpClient _http;
        private string? _adminKey;

        public bool IsAuthenticated { get; private set; }

        /// <summary>
        /// Tests a key against admin, brand, and supplier endpoints (in that order).
        /// Returns the first matching role, or null if no match.
        /// </summary>
        public async Task<(string role, string? name, int? id)?> TestKeyAsync(string key)
        {
            // 1. Test as admin
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/stats");
                request.Headers.Add("X-Admin-Key", key);
                var response = await _http.SendAsync(request);
                if (response.IsSuccessStatusCode)
                    return ("admin", "Admin", null);
            }
            catch { }

            // 2. Test as brand — verify the returned entity's api_key matches
            // (both /api/brands and /api/suppliers return 200 for any valid tenant key,
            //  but only the entity's own record will have a matching api_key)
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "/api/brands");
                request.Headers.Add("X-API-Key", key);
                var response = await _http.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var (name, id, apiKey) = ExtractNameAndId(json, "brand_name");
                    if (apiKey == key)
                        return ("brand", name, id);
                }
            }
            catch { }

            // 3. Test as supplier — same api_key match check
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "/api/suppliers");
                request.Headers.Add("X-API-Key", key);
                var response = await _http.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var (name, id, apiKey) = ExtractNameAndId(json, "supplier_name");
                    if (apiKey == key)
                        return ("supplier", name, id);
                }
            }
            catch { }

            return null;
        }

        /// <summary>
        /// Configures the ApiClient with an admin key (if any) and marks as authenticated.
        /// </summary>
        public void ConfigureSession(string? adminKey)
        {
            _adminKey = adminKey;
            _http.DefaultRequestHeaders.Remove("X-Admin-Key");
            if (adminKey != null)
                _http.DefaultRequestHeaders.Add("X-Admin-Key", adminKey);
            IsAuthenticated = true;
        }

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

        public async Task<string?> PostWithTenantKeyAsync(string endpoint, object data, string tenantApiKey)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Headers.Add("X-API-Key", tenantApiKey);
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

        public async Task<string?> PutWithTenantKeyAsync(string endpoint, object data, string tenantApiKey)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Put, endpoint);
                request.Headers.Add("X-API-Key", tenantApiKey);
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

        public async Task<string?> DeleteWithTenantKeyAsync(string endpoint, string tenantApiKey)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Delete, endpoint);
                request.Headers.Add("X-API-Key", tenantApiKey);
                var response = await _http.SendAsync(request);
                return await response.Content.ReadAsStringAsync();
            }
            catch
            {
                return null;
            }
        }

        private static (string? name, int? id, string? apiKey) ExtractNameAndId(string json, string nameField)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("data", out var data))
                {
                    JsonElement item;
                    if (data.ValueKind == JsonValueKind.Array && data.GetArrayLength() > 0)
                        item = data[0];
                    else if (data.ValueKind == JsonValueKind.Object)
                        item = data;
                    else
                        return (null, null, null);

                    string? name = item.TryGetProperty(nameField, out var nameProp) ? nameProp.GetString() : null;
                    int? id = item.TryGetProperty("id", out var idProp) && idProp.ValueKind == JsonValueKind.Number
                        ? idProp.GetInt32() : null;
                    string? apiKey = item.TryGetProperty("api_key", out var keyProp) ? keyProp.GetString() : null;
                    return (name, id, apiKey);
                }
            }
            catch { }
            return (null, null, null);
        }
    }
}
