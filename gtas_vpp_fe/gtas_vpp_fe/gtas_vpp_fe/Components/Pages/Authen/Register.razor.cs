using gtas_vpp_fe.Helpers;
using gtas_vpp_shared.DTOs.Req.Account;
using gtas_vpp_shared.DTOs.Res.Account;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;
using System.Text.Json;

namespace gtas_vpp_fe.Components.Pages.Authen;

public partial class Register
{
    [Inject] public IHttpClientFactory HttpClientFactory { get; set; } = default!;
    [Inject] public NavigationManager Navigation { get; set; } = default!;

    protected AccountRegistrationReqDTO Model { get; set; } = new();
    protected bool IsLoading { get; set; }
    protected string? ErrorMessage { get; set; }
    protected string? SuccessMessage { get; set; }

    private async Task SubmitAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        SuccessMessage = null;
        try
        {
            var client = HttpClientFactory.CreateClient(Config.HttpClientName);
            using var response = await client.PostAsJsonAsync(
                Config.ApiAccountRegisterEndpoint,
                Model);
            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage = await ReadErrorAsync(response);
                return;
            }

            var result = await response.Content.ReadFromJsonAsync<AccountLifecycleResDTO>();
            SuccessMessage = result?.Message
                ?? "Yêu cầu đã được tiếp nhận. Vui lòng chờ quản trị viên phê duyệt.";
            Model = new AccountRegistrationReqDTO();
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Không thể kết nối máy chủ. Vui lòng thử lại sau.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        try
        {
            using var json = JsonDocument.Parse(body);
            if (json.RootElement.TryGetProperty("message", out var message))
            {
            return message.GetString() ?? "Yêu cầu không hợp lệ.";
            }
        }
        catch (JsonException)
        {
            // Fall back to a safe generic message below.
        }

        return "Yêu cầu không hợp lệ. Vui lòng kiểm tra lại thông tin.";
    }
}
