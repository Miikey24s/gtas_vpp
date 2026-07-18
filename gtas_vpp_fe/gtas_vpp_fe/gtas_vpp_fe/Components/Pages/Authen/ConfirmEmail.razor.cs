using gtas_vpp_fe.Helpers;
using gtas_vpp_shared.DTOs.Req.Account;
using gtas_vpp_shared.DTOs.Res.Account;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;
using System.Text.Json;

namespace gtas_vpp_fe.Components.Pages.Authen;

public partial class ConfirmEmail
{
    [Inject] public IHttpClientFactory HttpClientFactory { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "userId")]
    public int UserId { get; set; }

    [SupplyParameterFromQuery(Name = "token")]
    public string? Token { get; set; }

    protected bool IsLoading { get; private set; } = true;
    protected string? ErrorMessage { get; private set; }
    protected string? SuccessMessage { get; private set; }

    protected override async Task OnInitializedAsync()
    {
        // Confirmation changes server state. Never issue it once during
        // prerendering and a second time when the interactive circuit starts.
        if (!RendererInfo.IsInteractive)
        {
            return;
        }

        if (UserId <= 0 || string.IsNullOrWhiteSpace(Token))
        {
            ErrorMessage = "Liên kết xác nhận không hợp lệ hoặc đã hết hạn.";
            IsLoading = false;
            return;
        }

        try
        {
            var client = HttpClientFactory.CreateClient(Config.HttpClientName);
            var endpoint = $"{Config.ApiAccountConfirmEmailEndpoint}?userId={UserId}&token={Uri.EscapeDataString(Token)}";
            using var response = await client.GetAsync(endpoint);
            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage = await ReadMessageAsync(response);
                return;
            }

            var result = await response.Content.ReadFromJsonAsync<AccountLifecycleResDTO>();
            SuccessMessage = result?.Message ?? "Email đã được xác nhận.";
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

    private static async Task<string> ReadMessageAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        try
        {
            using var json = JsonDocument.Parse(body);
            if (json.RootElement.TryGetProperty("message", out var message))
            {
                return message.GetString() ?? "Liên kết xác nhận không hợp lệ hoặc đã hết hạn.";
            }
        }
        catch (JsonException)
        {
            // Use the generic message below.
        }

        return "Liên kết xác nhận không hợp lệ hoặc đã hết hạn.";
    }
}
