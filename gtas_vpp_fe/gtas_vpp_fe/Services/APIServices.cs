using gtas_vpp_fe.Helpers.DTOs;
using System.Net.Http.Json;
namespace gtas_vpp_fe.Services
{
    public interface IAPIServices
    {
        void SetBaseUrl(string baseUrl);
        Task<string> GetDataFromExternalApiAsync(string endpoint);
        Task<sp_ResDTOclient> aPIFrom_sp_Authen(string sptype, object body, string? baseurl = null);
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
        public async Task<sp_ResDTOclient> aPIFrom_sp_Authen(string sptype, object body, string? baseurl = null)
        {
            if (_httpClient.BaseAddress == null)
            {
                if(baseurl != null) 
                _httpClient.BaseAddress = new Uri(baseurl);
            }
            var url = $"{_rootUrl}sp_Authen?sptype={sptype}";
            var response = await _httpClient.PostAsJsonAsync(url, body);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<sp_ResDTOclient>();
                return result!;
            }
            else
            {
                return new sp_ResDTOclient
                {
                    IsSuccess = false,
                    ErrorMess = $"Error: {response.StatusCode}, {response.ReasonPhrase}"
                };
            }
        }
    }
}
