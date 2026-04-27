using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.AI;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace gtas_vpp_fe.Components.AI;

public partial class AIChatBox
{
    [Inject] private IAPIServices ApiServices { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    private bool _isOpen;
    private string _userInput = string.Empty;
    private bool _isLoading;
    private bool _pendingScroll;
    private ElementReference _messagesContainer;
    private readonly List<ChatMessage> _messages = [];

    private void ToggleChat()
    {
        _isOpen = !_isOpen;
        _pendingScroll = _isOpen;
    }

    private async Task HandleInputKeyDown(KeyboardEventArgs args)
    {
        if (args.Key == "Enter" && !_isLoading)
        {
            await SendMessage();
        }
    }

    private async Task SendMessage()
    {
        if (string.IsNullOrWhiteSpace(_userInput) || _isLoading)
        {
            return;
        }

        var userMessage = _userInput.Trim();
        var history = _messages
            .Where(m => m.Role is "user" or "assistant")
            .TakeLast(10)
            .Select(m => new AIChatMessageDTO
            {
                Role = m.Role,
                Content = m.Content,
                Timestamp = m.Timestamp
            })
            .ToList();

        _userInput = string.Empty;
        _messages.Add(new ChatMessage
        {
            Role = "user",
            Content = userMessage,
            Timestamp = DateTime.Now
        });

        _isLoading = true;
        _pendingScroll = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            var request = new AIChatRequestDTO
            {
                Message = userMessage,
                History = history
            };

            var result = await ApiServices.PostFromApiAsync<AIChatResponseDTO>("api/AI/chat", request);
            var responseText = result?.IsSuccess == true
                ? result.Message
                : result?.ErrorMessage ?? result?.Message ?? "Lỗi kết nối";

            _messages.Add(new ChatMessage
            {
                Role = "assistant",
                Content = string.IsNullOrWhiteSpace(responseText) ? "Không có phản hồi từ dịch vụ AI." : responseText,
                SuggestedItems = result?.SuggestedItems,
                Timestamp = DateTime.Now
            });
        }
        catch
        {
            _messages.Add(new ChatMessage
            {
                Role = "assistant",
                Content = "Không thể kết nối tới dịch vụ AI. Vui lòng thử lại sau.",
                Timestamp = DateTime.Now
            });
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
