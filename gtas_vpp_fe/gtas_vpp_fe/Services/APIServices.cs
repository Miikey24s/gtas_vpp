using gtas_vpp_fe.Helpers;
using gtas_vpp_shared.DTOs;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace gtas_vpp_fe.Services
{
    public interface IAPIServices
    {
        Task SetBaseUrl(string baseUrl);
        Task<string> GetDataFromExternalApiAsync(string endpoint);
        Task<sp_ResDTO> aPIFrom_sp_Authen(string sptype, object body, string? baseurl = null, JsonSerializerOptions? jsonOptions = null);
        Task<T?> APIFrom_sp_Authen_Typed<T>(string sptype, object body, string? baseurl = null, JsonSerializerOptions? jsonOptions = null);
        Task<T?> GetFromApiAsync<T>(string endpoint);
        Task<(T? Data, int TotalCount)> GetFromApiWithTotalCountAsync<T>(string endpoint);
        Task<(T? Data, int TotalCount, int TotalLines, int TotalQty)> GetFromApiWithStatsAsync<T>(string endpoint);
        Task<T?> PostFromApiAsync<T>(string endpoint, object? body);
        Task<T?> PutFromApiAsync<T>(string endpoint, object body);
        Task<T?> PatchFromApiAsync<T>(string endpoint, object body);
        Task<bool> DeleteFromApiAsync(string endpoint);
    }
    public class APIServices : IAPIServices
    {
        private readonly HttpClient _httpClient;
        private readonly AuthenticationStateProvider _authProvider;
        private readonly string _rootUrl = "api/SQL/StoreProcedure/";
        public APIServices(HttpClient httpClient, AuthenticationStateProvider authProvider)
        {
            _httpClient = httpClient;
            _authProvider = authProvider;
        }

        private async Task ApplyAuthorizationHeaderAsync()
        {
            var authState = await _authProvider.GetAuthenticationStateAsync();
            var token = authState.User.Claims.Get(ClaimKeys.AccessToken);

            if (!string.IsNullOrWhiteSpace(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            else
            {
                _httpClient.DefaultRequestHeaders.Authorization = null;
            }
        }

        public Task SetBaseUrl(string baseUrl)
        {
            if (_httpClient.BaseAddress == null)
            {
                _httpClient.BaseAddress = new Uri(baseUrl);
            }

            return Task.CompletedTask;
        }
        private async Task EnsureSuccessWithDetailsAsync(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var errorMessage = $"API Error: {response.StatusCode}";
                try
                {
                    // Attempt to parse standard ProblemDetails or custom error JSON
                    var errorObj = JsonSerializer.Deserialize<JsonElement>(content);
                    if (errorObj.TryGetProperty("message", out var msg))
                        errorMessage = msg.GetString() ?? errorMessage;
                    else if (errorObj.TryGetProperty("title", out var title))
                        errorMessage = title.GetString() ?? errorMessage;
                }
                catch
                {
                    // If parsing fails, use raw content if it's short, else keep status code
                    if (!string.IsNullOrWhiteSpace(content) && content.Length < 200)
                        errorMessage = content;
                }
                throw new HttpRequestException(errorMessage);
            }
        }

        public async Task<string> GetDataFromExternalApiAsync(string endpoint)
        {
            await ApplyAuthorizationHeaderAsync();
            var response = await _httpClient.GetAsync(_rootUrl + endpoint);
            await EnsureSuccessWithDetailsAsync(response);
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<sp_ResDTO> aPIFrom_sp_Authen(string sptype, object body, string? url = null, JsonSerializerOptions? jsonOptions = null)
        {
            if (url == null) url = $"{_rootUrl}sp_Authen?sptype={sptype}";

            await ApplyAuthorizationHeaderAsync();
            var content = JsonContent.Create(body, options: jsonOptions);
            var response = await _httpClient.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<sp_ResDTO>();
                return result!;
            }
            else
            {
                return new sp_ResDTO
                {
                    IsSuccess = false,
                    ErrorMess = $"Error: {response.StatusCode}, {response.ReasonPhrase}"
                };
            }
        }

        public async Task<T?> APIFrom_sp_Authen_Typed<T>(string sptype, object body, string? url = null, JsonSerializerOptions? jsonOptions = null)
        {
            var apiResult = await aPIFrom_sp_Authen(sptype, body, url, jsonOptions);

            if (apiResult == null || !apiResult.IsSuccess || string.IsNullOrWhiteSpace(apiResult.ResData))
            {
                return default;
            }

            try
            {
                return JsonSerializer.Deserialize<T>(
                    apiResult.ResData,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch
            {
                return default;
            }
        }

        public async Task<T?> GetFromApiAsync<T>(string endpoint)
        {
            await ApplyAuthorizationHeaderAsync();
            var response = await _httpClient.GetAsync(endpoint);
            await EnsureSuccessWithDetailsAsync(response);
            return await ReadResponseAsJsonAsync<T>(response);
        }

        public async Task<(T? Data, int TotalCount)> GetFromApiWithTotalCountAsync<T>(string endpoint)
        {
            await ApplyAuthorizationHeaderAsync();
            var response = await _httpClient.GetAsync(endpoint);
            await EnsureSuccessWithDetailsAsync(response);

            var totalCount = 0;
            if (response.Headers.TryGetValues("X-Total-Count", out var headerValues))
            {
                int.TryParse(headerValues.FirstOrDefault(), out totalCount);
            }

            var data = await ReadResponseAsJsonAsync<T>(response);
            return (data, totalCount);
        }

        public async Task<(T? Data, int TotalCount, int TotalLines, int TotalQty)> GetFromApiWithStatsAsync<T>(string endpoint)
        {
            await ApplyAuthorizationHeaderAsync();
            var response = await _httpClient.GetAsync(endpoint);
            await EnsureSuccessWithDetailsAsync(response);

            int totalCount = 0, totalLines = 0, totalQty = 0;
            if (response.Headers.TryGetValues("X-Total-Count", out var countVals))
                int.TryParse(countVals.FirstOrDefault(), out totalCount);
            if (response.Headers.TryGetValues("X-Total-Lines", out var lineVals))
                int.TryParse(lineVals.FirstOrDefault(), out totalLines);
            if (response.Headers.TryGetValues("X-Total-Qty", out var qtyVals))
                int.TryParse(qtyVals.FirstOrDefault(), out totalQty);

            var data = await ReadResponseAsJsonAsync<T>(response);
            return (data, totalCount, totalLines, totalQty);
        }

        public async Task<T?> PostFromApiAsync<T>(string endpoint, object? body)
        {
            await ApplyAuthorizationHeaderAsync();
            var response = await _httpClient.PostAsJsonAsync(endpoint, body);
            await EnsureSuccessWithDetailsAsync(response);
            return await ReadResponseAsJsonAsync<T>(response);
        }

        public async Task<T?> PutFromApiAsync<T>(string endpoint, object body)
        {
            await ApplyAuthorizationHeaderAsync();
            var response = await _httpClient.PutAsJsonAsync(endpoint, body);
            await EnsureSuccessWithDetailsAsync(response);
            return await ReadResponseAsJsonAsync<T>(response);
        }

        public async Task<T?> PatchFromApiAsync<T>(string endpoint, object body)
        {
            await ApplyAuthorizationHeaderAsync();
            var request = new HttpRequestMessage(HttpMethod.Patch, endpoint)
            {
                Content = JsonContent.Create(body)
            };

            var response = await _httpClient.SendAsync(request);
            await EnsureSuccessWithDetailsAsync(response);
            return await ReadResponseAsJsonAsync<T>(response);
        }

        private static async Task<T?> ReadResponseAsJsonAsync<T>(HttpResponseMessage response)
        {
            if (response.Content == null)
            {
                return default;
            }

            var content = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(content))
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        public async Task<bool> DeleteFromApiAsync(string endpoint)
        {
            await ApplyAuthorizationHeaderAsync();
            var response = await _httpClient.DeleteAsync(endpoint);
            return response.IsSuccessStatusCode;
        }
    }
}

