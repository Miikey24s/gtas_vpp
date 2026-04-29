using gtas_vpp_shared.DTOs.AI;

namespace gtas_vpp_be.AI.KeyManagement.Interfaces;

public interface IGeminiKeyManager
{
    /// <summary>
    /// Khởi tạo danh sách các Key.
    /// </summary>
    void InitializeKeys(IEnumerable<string> keys, IEnumerable<string>? accounts = null);

    /// <summary>
    /// Bóc tách thông tin Rate Limit từ HTTP Response Headers.
    /// </summary>
    void UpdateKeyStatsFromHeaders(string key, HttpResponseMessage response);

    /// <summary>
    /// Thực hiện ping API chủ động để kiểm tra Key còn sống hay chết (Live/Dead).
    /// </summary>
    Task<KeyStatus> CheckKeyHealthAsync(string key);

    /// <summary>
    /// Đánh dấu Key bị giới hạn (exhausted) cho một model cụ thể trong khoảng thời gian nhất định.
    /// </summary>
    void MarkKeyExhausted(string key, string model, TimeSpan cooldown);

    /// <summary>
    /// Kiểm tra xem Key có đang bị giới hạn cho model đó hay không.
    /// </summary>
    bool IsKeyExhausted(string key, string model);

    /// <summary>
    /// Lấy danh sách thống kê toàn bộ các Key.
    /// </summary>
    IEnumerable<GeminiKeyInfo> GetAllKeyInfos();
}
