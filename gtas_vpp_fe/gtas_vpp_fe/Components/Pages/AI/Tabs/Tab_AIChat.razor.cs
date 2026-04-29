using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.AI;
using gtas_vpp_shared.DTOs.Res.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Security.Claims;

namespace gtas_vpp_fe.Components.Pages.AI.Tabs
{
    public partial class Tab_AIChat
    {
        [Parameter] public IEnumerable<Claim> claims { get; set; } = Enumerable.Empty<Claim>();
        [Parameter] public sp_Authentication_GetPermissionSinglePage sp_Authentication_GetPermissionSinglePage { get; set; } = new();

        [Inject] public IAPIServices ApiServices { get; set; } = default!;
        [Inject] public IJSRuntime JS { get; set; } = default!;


        private string _userInput = string.Empty;
        private bool _isLoading;
        private bool _pendingScroll;
        private ElementReference _messagesContainer;
        private readonly List<ChatMessage> _messages = [];

        private async Task HandleKey(KeyboardEventArgs args)
        {
            if (args.Key == "Enter" && !_isLoading)
                await Send();
        }

        private async Task Send()
        {
            if (string.IsNullOrWhiteSpace(_userInput) || _isLoading) return;

            var userMessage = _userInput.Trim();
            var history = _messages
                .Where(m => m.Role is "user" or "assistant")
                .TakeLast(10)
                .Select(m => new AIChatMessageDTO { Role = m.Role, Content = m.Content, Timestamp = m.Timestamp })
                .ToList();

            _userInput = string.Empty;
            _messages.Add(new ChatMessage { Role = "user", Content = userMessage, Timestamp = DateTime.Now });
            _isLoading = true;
            _pendingScroll = true;
            await InvokeAsync(StateHasChanged);

            try
            {
                var result = await ApiServices.PostFromApiAsync<AIChatResponseDTO>("api/AI/chat", new AIChatRequestDTO
                {
                    Message = userMessage,
                    History = history
                });

                var text = result?.IsSuccess == true
                    ? result.Message
                    : result?.ErrorMessage ?? result?.Message ?? "Lỗi kết nối";

                _messages.Add(new ChatMessage
                {
                    Role = "assistant",
                    Content = string.IsNullOrWhiteSpace(text) ? "Không có phản hồi từ AI." : text,
                    SuggestedItems = result?.SuggestedItems,
                    Timestamp = DateTime.Now
                });
            }
            catch
            {
                _messages.Add(new ChatMessage { Role = "assistant", Content = "Không thể kết nối tới dịch vụ AI.", Timestamp = DateTime.Now });
            }
            finally
            {
                _isLoading = false;
                _pendingScroll = true;
                await InvokeAsync(StateHasChanged);
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (_pendingScroll)
            {
                _pendingScroll = false;
                await JS.InvokeVoidAsync("gtasVppAiChat.scrollToBottom", _messagesContainer);
            }
        }

        private sealed class ChatMessage
        {
            public string Role { get; set; } = string.Empty;
            public string Content { get; set; } = string.Empty;
            public List<AISuggestedItemDTO>? SuggestedItems { get; set; }
            public DateTime Timestamp { get; set; }
        }
    }
}
