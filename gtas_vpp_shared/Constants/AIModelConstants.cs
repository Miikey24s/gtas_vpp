using System.Collections.Generic;
using gtas_vpp_shared.DTOs.AI;

namespace gtas_vpp_shared.Constants;

public static class AIModelConstants
{
    public static readonly List<AITierModel> Tiers = new()
    {
        new AITierModel
        {
            TierLevel = 1,
            DisplayName = "Gemini 3 Flash",
            ActualModelName = "gemini-2.5-pro",
            Role = "Xử lý các câu hỏi phức tạp, yêu cầu suy luận sâu.",
            MaxRpd = 20,
            RpmLimit = 2,
            ContextWindow = "2 Million Tokens",
            Features = "Function Calling, JSON Mode, Advanced Reasoning, Vision"
        },
        new AITierModel
        {
            TierLevel = 2,
            DisplayName = "Gemini 3.1 Flash Lite",
            ActualModelName = "gemini-3.1-flash-lite-preview",
            Role = "Gánh 99% lượng chat và code thông thường, tốc độ siêu tốc.",
            MaxRpd = 500,
            RpmLimit = 15,
            ContextWindow = "1 Million Tokens",
            Features = "Function Calling, Fast Generation, Vision"
        },
        new AITierModel
        {
            TierLevel = 3,
            DisplayName = "Gemma 4 31B",
            ActualModelName = "gemini-2.0-flash", // Dùng tạm model này thay thế vì Google API không support Gemma trực tiếp qua endpoint openai
            Role = "Chốt chặn an toàn khi chạy các luồng tự động (auto-scripts).",
            MaxRpd = 1500,
            RpmLimit = 15,
            ContextWindow = "1 Million Tokens",
            Features = "JSON Mode, Text-to-text"
        }
    };
}
