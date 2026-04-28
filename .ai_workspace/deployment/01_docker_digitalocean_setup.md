# GTAS VPP - Docker Deployment Guide & Configuration

## 1. Mục đích
Tài liệu này lưu trữ các quyết định kiến trúc và cấu hình để deploy hệ thống GTAS VPP lên DigitalOcean Droplet (8GB RAM - 4 vCPUs).

## 2. Kiến trúc Deployment
Sử dụng `docker-compose` All-in-One với 4 services chính:
- **db**: SQL Server 2022 Express. Được cấu hình giới hạn RAM (1.5GB) qua biến `MSSQL_MEMORY_LIMIT_MB` để nhường RAM cho AI.
- **backend**: .NET 10 Web API. Build qua Multi-stage. Tối ưu RAM bằng Workstation GC (`DOTNET_gcServer=0`).
- **frontend**: Blazor Server. Chạy trên .NET 10, cấu hình tối ưu GC tương tự backend. Không sử dụng Nginx tĩnh vì Blazor đang chạy chế độ `InteractiveServerRenderMode`.
- **ollama** (Tùy chọn): Chạy thuần CPU, sử dụng model nhẹ (`gemma:2b` và `nomic-embed-text`).

Nginx được cài đặt trực tiếp trên Host (không qua Docker) để làm Reverse Proxy (port 80/443), hỗ trợ SSL (Let's Encrypt) và WebSocket (`/_blazor`) cho Blazor Server SignalR.

## 3. Cấu hình AI (Dual Mode)
Hệ thống hỗ trợ 2 chế độ AI, cấu hình qua file `.env`:

### Mode 1: Google AI (Mặc định & Khuyến nghị)
- **Cấu hình**: `AI_PROVIDER=Google`
- **Models**: `gemini-2.5-flash` (Chat) và `text-embedding-004` (Embed).
- **Ưu điểm**: Tiết kiệm RAM (~3GB trống trên Droplet 8GB), tốc độ phản hồi cực nhanh, không phụ thuộc tài nguyên máy chủ.

### Mode 2: Ollama Self-hosted
- **Cấu hình**: `AI_PROVIDER=Ollama`, Profile Docker: `--profile ollama`
- **Models**: `gemma:2b` (Chat) và `nomic-embed-text` (Embed).
- **Ưu điểm**: Hoàn toàn nội bộ (offline), không tốn phí API.
- **Lệnh Pull Models**:
  ```bash
  docker exec gtas-vpp-ollama ollama pull gemma:2b
  docker exec gtas-vpp-ollama ollama pull nomic-embed-text
  ```

## 4. Các điểm cần lưu ý (Gotchas)
- **Health check cho AI**: Lớp `DefaultAIOrchestrator.cs` đã được cập nhật để cho phép bỏ qua check kết nối cloud, đồng thời ping đúng hostname `ollama` khi chạy trong mạng nội bộ Docker.
- **.dockerignore**: Bắt buộc **KHÔNG** exclude thư mục `**/Migrations`. Nếu exclude, .NET SDK trong Docker sẽ không tìm thấy assembly migration và báo lỗi EF Core.
- **Cấu hình Nginx Proxy**: Bắt buộc phải có các directive `Upgrade $http_upgrade` và `Connection $connection_upgrade` ở endpoint `/_blazor` để Blazor Server (SignalR) có thể duy trì kết nối WebSocket.
