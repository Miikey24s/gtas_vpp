Bạn là AI Worker (Codex) trong hệ thống AI Integration cho dự án GTAS VPP.

# Bắt buộc đọc trước khi làm
- `.ai_workspace/ai_integration/architecture.md` — chuẩn AI integration
- `.ai_workspace/ai_integration/context.md` — context project
- `.ai_workspace/ai_integration/tasks.json` — P8-A + P8-B phải done

# Nhiệm vụ: Thực thi Phase P8-C (Frontend + Polish — Tasks P8.9 → P8.10)

## P8.9 — Blazor AIChatBox Component

### Tạo `Components/AI/AIChatBox.razor`

Floating chatbox component gắn vào MainLayout. Design:
- **FAB button**: Góc phải dưới, fixed position, icon chat (💬 hoặc Material Symbol "chat")
- **Panel**: Click FAB → slide-up panel 380px width × 500px height
- **Header**: "AI Trợ lý VPP" + nút đóng (X)
- **Message area**: Scroll, user bubbles (phải, xanh), AI bubbles (trái, xám)
- **Input**: TextBox + Send button ở bottom
- **Loading**: "AI đang suy nghĩ..." indicator khi đợi response
- **Suggested items**: Khi AI trả về SuggestedItems → render dạng mini card list

### Logic (AIChatBox.razor.cs)
```csharp
// Inject
@inject HttpClient Http  // hoặc IHttpClientFactory nếu đã setup
@inject NavigationManager Navigation

// State
private bool _isOpen = false;
private string _userInput = "";
private bool _isLoading = false;
private List<ChatMessage> _messages = new();

// Struct
private class ChatMessage
{
    public string Role { get; set; } // "user" or "assistant"
    public string Content { get; set; }
    public List<AISuggestedItemDTO>? SuggestedItems { get; set; }
    public DateTime Timestamp { get; set; }
}

// Methods
private void ToggleChat() => _isOpen = !_isOpen;

private async Task SendMessage()
{
    if (string.IsNullOrWhiteSpace(_userInput)) return;
    
    var userMsg = _userInput;
    _userInput = "";
    _messages.Add(new ChatMessage { Role = "user", Content = userMsg, Timestamp = DateTime.Now });
    _isLoading = true;
    StateHasChanged();
    
    try
    {
        // Build request with history
        var request = new AIChatRequestDTO
        {
            Message = userMsg,
            History = _messages.Where(m => m.Role is "user" or "assistant")
                              .TakeLast(10)  // max 10 messages history
                              .Select(m => new AIChatMessageDTO { Role = m.Role, Content = m.Content })
                              .ToList()
        };
        
        // Call BE API
        var response = await Http.PostAsJsonAsync("/api/ai/chat", request);
        var result = await response.Content.ReadFromJsonAsync<AIChatResponseDTO>();
        
        _messages.Add(new ChatMessage
        {
            Role = "assistant",
            Content = result?.Message ?? "Lỗi kết nối",
            SuggestedItems = result?.SuggestedItems,
            Timestamp = DateTime.Now
        });
    }
    catch (Exception ex)
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
        StateHasChanged();
        // Auto scroll to bottom
    }
}
```

### CSS (AIChatBox.razor.css)
Key styles:
- `.ai-fab` — fixed bottom-right, round button, z-index 1000, box-shadow
- `.ai-chat-panel` — fixed bottom-right (above fab), border-radius 12px, box-shadow, slide-up animation
- `.ai-chat-header` — gradient background, white text, flex between title + close button
- `.ai-message-user` — align right, blue/teal background, white text, rounded corners
- `.ai-message-assistant` — align left, light gray background, dark text, rounded corners
- `.ai-typing-indicator` — pulse animation dots
- `.ai-suggested-item` — small card with VPP info, border, padding
- Animation: `@keyframes slideUp` from translateY(100%) to translateY(0)

### Modify MainLayout.razor
Thêm `<AIChatBox />` sau `</RadzenLayout>`:
```razor
<RadzenLayout ...>
    <LeftSidebar />
    <RadzenBody>
        @Body
    </RadzenBody>
</RadzenLayout>
<AIChatBox />
```

### HttpClient setup
FE dùng Blazor Server → cần setup HttpClient cho API calls.
Check trong `Program.cs` FE xem đã có `builder.Services.AddHttpClient()` chưa.
Nếu chưa → thêm:
```csharp
builder.Services.AddHttpClient("AI", client =>
{
    client.BaseAddress = new Uri(Configuration["ApiSettings:BaseUrl"]);
});
```
Hoặc reuse existing `APIServices.cs` pattern nếu có.

## P8.10 — Architecture Update + Final Verify

### Modify `.ai_workspace/refactor/architecture.md`
Thêm section cuối file:
```markdown
## AI Integration (Phase 8)

### Framework
- `Microsoft.Extensions.AI` (M.E.AI) — KHÔNG Semantic Kernel
- Abstractions: `IChatClient`, `IEmbeddingGenerator<string, Embedding<float>>`
- Provider: Ollama (chạy trên PC qua LAN)

### Architecture
- Interface: `gtas_vpp_be.Service/AI/`
- Implementation: `gtas_vpp_be.AI/` (new project)
- DI: `AddGtasAIServices()` extension method

### Models (Ollama)
| Purpose | Model | VRAM |
|---------|-------|------|
| Text generation | gemma4:e4b | ~5-6GB |
| Embedding | nomic-embed-text | ~550MB |

### Quy tắc
- AI service KHÔNG access VPPContext trực tiếp → dùng AIDbContext riêng
- Response có fallback khi Ollama unavailable
- Embedding cache invalidate khi VPP catalog thay đổi
```

### Verify E2E
1. `dotnet build gtas_vpp/gtas_vpp.slnx` — 0 errors
2. `dotnet test code-be/gtas_vpp_be.Tests/` — all pass
3. Start BE → test Swagger `/api/ai/health`
4. `POST /api/ai/rebuild-embeddings` → verify DB has embeddings
5. `POST /api/ai/chat` → verify response
6. Start FE → verify chatbox visible + functional

### Report
Ghi tổng hợp vào `.ai_workspace/ai_integration/reports/P8C_frontend.md`

# Quy tắc
- FE KHÔNG reference BE project trực tiếp — chỉ qua gtas_vpp_shared DTOs
- Dùng Radzen components khi phù hợp, custom CSS cho chat-specific UI
- HttpClient phải gửi JWT token (reuse auth pattern hiện có)
- CSS scoped (component-level), KHÔNG sửa global app.css
