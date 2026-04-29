# Radzen Blazor MCP Client

Simple MCP client để test Radzen Blazor MCP server.

## Setup

1. **Lấy API key** (miễn phí):
   - Truy cập: https://www.radzen.com/blazor-mcp/documentation/get-your-key
   - Đăng ký email → nhận 50 requests/15 ngày

2. **Cài đặt dependencies**:
   ```bash
   pip install requests
   ```

3. **Cấu hình API key** (chọn 1 trong 3 cách):
   ```bash
   # Cách 1: Thêm vào .env file (khuyến nghị)
   echo "RADZEN_API_KEY=your_key_here" >> .env
   
   # Cách 2: Set environment variable
   export RADZEN_API_KEY=your_key_here
   
   # Cách 3: Pass trực tiếp vào command
   python3 radzen_mcp_client.py YOUR_KEY init
   ```

## Sử dụng

### 1. Direct MCP Client (cho developers)

```bash
# Initialize và xem server capabilities
python3 radzen_mcp_client.py init

# List tất cả tools có sẵn
python3 radzen_mcp_client.py tools

# Search tài liệu Radzen Blazor
python3 radzen_mcp_client.py search "RadzenDataGrid"
python3 radzen_mcp_client.py search "How to create CRUD pages"
```

### 2. AI Query Wrapper (cho AI assistants)

Script này được thiết kế để **các AI tools khác** (ChatGPT, Claude, Gemini, v.v.) có thể gọi:

```bash
# Query từ command line
python3 radzen_query.py "How to use RadzenDialog"
python3 radzen_query.py "Form validation examples"
python3 radzen_query.py "DataGrid with filtering and sorting"
```

**Từ AI assistant:**
```python
# AI có thể gọi subprocess
import subprocess
result = subprocess.run(
    ["python3", "radzen_query.py", "How to create a DataGrid with CRUD"],
    capture_output=True, text=True
)
print(result.stdout)  # Formatted documentation
```

## Ví dụ output

```json
{
  "result": {
    "content": [
      {
        "type": "text",
        "text": "# Generate CRUD Pages with RadzenDataGrid\n\n..."
      }
    ]
  }
}
```

## Tích hợp với Kiro CLI

Kiro CLI có thể gọi `radzen_query.py` để truy vấn tài liệu Radzen Blazor khi cần:

```bash
# Trong Kiro CLI session
!python3 radzen_query.py "RadzenChart examples"
```

## Giới hạn

- **Free tier**: 50 requests / 15 ngày
- **Timeout**: 30 giây mỗi query
- **Rate limit**: Tuân thủ fair use policy của Radzen
