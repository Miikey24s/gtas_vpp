using gtas_vpp_shared.DTOs.Req.VPP;
using gtas_vpp_shared.DTOs.Res.VPP;

namespace gtas_vpp_fe.Components.Pages.VPPRequest.Components
{
    /// <summary>
    /// Trạng thái dùng chung của luồng vận hành kỳ trong một circuit: bước Chọn nguồn cung
    /// (supply-allocation) tạo bản xem trước, bước Chốt kỳ (settlement-flow) xác nhận đúng
    /// bản đó qua InputHash + PriceAsOfUtc + idempotency key. Scoped per circuit —
    /// InteractiveServer global nên mỗi phiên người dùng có một bản riêng.
    /// </summary>
    public sealed class PeriodSettlementState
    {
        public SettlementPreviewResDTO? Preview { get; private set; }
        public List<SettlementExceptionReqDTO> Exceptions { get; } = [];
        public string? IdempotencyKey { get; private set; }
        public Guid? SelectedSupplierId { get; set; }

        public event Action? Changed;

        public void SetPreview(SettlementPreviewResDTO? preview)
        {
            Preview = preview;
            if (preview is not null)
            {
                IdempotencyKey ??= $"{preview.Year:D4}{preview.Month:D2}-{Guid.NewGuid():N}";
            }

            Changed?.Invoke();
        }

        public void Reset()
        {
            Preview = null;
            Exceptions.Clear();
            IdempotencyKey = null;
            SelectedSupplierId = null;
            Changed?.Invoke();
        }

        /// <summary>Sau khi chốt/hiệu chỉnh thành công, khóa idempotency phải đổi cho lần sau.</summary>
        public void CompleteConfirmation()
        {
            IdempotencyKey = null;
            Changed?.Invoke();
        }
    }
}
