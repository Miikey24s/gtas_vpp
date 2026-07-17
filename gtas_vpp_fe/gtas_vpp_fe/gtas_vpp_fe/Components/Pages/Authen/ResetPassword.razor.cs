using gtas_vpp_fe.Helpers;
using gtas_vpp_shared.DTOs.Req.Account;
using gtas_vpp_shared.DTOs.Res.Account;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;
using System.Text.Json;

namespace gtas_vpp_fe.Components.Pages.Authen;

public partial class ResetPassword
{
    [Inject] public IHttpClientFactory HttpClientFactory { get; set; } = default!;
    [Inject] public NavigationManager Navigation { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "userId")]
    public int UserId { get; set; }

    [SupplyParameterFromQuery(Name = "token")]
    public string? Token { get; set; }

    protected PasswordResetReqDTO Model { get; set; } = new();
    protected bool IsLoading { get; set; }
    protected string? ErrorMessage { get; set; }
    protected string? SuccessMessage { get; set; }

    protected override void OnParametersSet()
    {
        Model.UserId = UserId;
        Model.Token = Token ?? string.Empty;
    }

    private async Task SubmitAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        SuccessMessage = null;
        try
        {
            var client = HttpClientFactory.CreateClient(Config.HttpClientName);
            using var response = await client.PostAsJsonAsync(
                Config.ApiAccountResetPasswordEndpoint,
                Model);
            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage = await ReadMessageAsync(response);
                return;
            }

            SuccessMessage = "Đặt lại mật khẩu thành công. Bạn có thể đăng nhập lại.";
            await Task.Delay(500);
            Navigation.NavigateTo(Config.LoginPagePath, forceLoad: true);
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
                return message.GetString() ?? "Liên kết đặt lại không hợp lệ hoặc đã hết hạn.";
            }
        }
        catch (JsonException)
        {
            // Use the generic message below.
        }

        return "Liên kết đặt lại không hợp lệ hoặc đã hết hạn.";
    }
}
