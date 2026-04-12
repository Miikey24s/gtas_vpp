using gtas_vpp_shared.DTOs;
using System.Net.Http.Json;
using System.Text.Json;
namespace gtas_vpp_fe.Services
{
    public interface IAPIServices
    {
        void SetBaseUrl(string baseUrl);
        Task<string> GetDataFromExternalApiAsync(string endpoint);
        Task<sp_ResDTO> aPIFrom_sp_Authen(string sptype, object body, string? baseurl = null, JsonSerializerOptions? jsonOptions = null);
        Task<T?> APIFrom_sp_Authen_Typed<T>(string sptype, object body, string? baseurl = null, JsonSerializerOptions? jsonOptions = null);
        Task<T?> GetFromApiAsync<T>(string endpoint);
        Task<T?> PostFromApiAsync<T>(string endpoint, object body);
        Task<T?> PutFromApiAsync<T>(string endpoint, object body);
        Task<T?> PatchFromApiAsync<T>(string endpoint, object body);
        Task<bool> DeleteFromApiAsync(string endpoint);
    }
    public class APIServices : IAPIServices
    {
        private readonly HttpClient _httpClient;
        private readonly string _rootUrl = "api/SQL/StoreProcedure/";
        public APIServices(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        public async void SetBaseUrl(string baseUrl)
        {
            if (_httpClient.BaseAddress == null)
            {
                _httpClient.BaseAddress = new Uri(baseUrl);
            }
        }
        public async Task<string> GetDataFromExternalApiAsync(string endpoint)
        {
            var response = await _httpClient.GetAsync(_rootUrl + endpoint);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        public async Task<sp_ResDTO> aPIFrom_sp_Authen(string sptype, object body, string? url = null, JsonSerializerOptions? jsonOptions = null)
        {
            //if (_httpClient.BaseAddress == null)
            //{
            //    if(baseurl != null) 
            //    _httpClient.BaseAddress = new Uri(baseurl);
            //}
            if (url == null) url = $"{_rootUrl}sp_Authen?sptype={sptype}";

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
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ;
            }
            catch (Exception ex)
            {
                return default;
            }
        }

        public async Task<T?> GetFromApiAsync<T>(string endpoint)
        {
            return await _httpClient.GetFromJsonAsync<T>(endpoint);
        }
        public async Task<T?> PostFromApiAsync<T>(string endpoint, object body)
        {
            var response = await _httpClient.PostAsJsonAsync(endpoint, body);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>();
        }

        public async Task<T?> PutFromApiAsync<T>(string endpoint, object body)
        {
            var response = await _httpClient.PutAsJsonAsync(endpoint, body);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>();
        }

        public async Task<T?> PatchFromApiAsync<T>(string endpoint, object body)
        {
            var request = new HttpRequestMessage(HttpMethod.Patch, endpoint)
            {
                Content = JsonContent.Create(body)
            };

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>();
        }

        public async Task<bool> DeleteFromApiAsync(string endpoint)
        {
            var response = await _httpClient.DeleteAsync(endpoint);
            return response.IsSuccessStatusCode;
        }
    }
}

