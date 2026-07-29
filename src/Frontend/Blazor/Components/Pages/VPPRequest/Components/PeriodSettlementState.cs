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
        public int Year { get; private set; }
        public int Month { get; private set; }
        public bool HasPeriod => Year >= 2024 && Month is >= 1 and <= 12;
        public SettlementPreviewResDTO? Preview { get; private set; }
        public List<SettlementExceptionReqDTO> Exceptions { get; } = [];
        public string? IdempotencyKey { get; private set; }
        public Guid? SelectedSupplierId { get; set; }

        public event Action? Changed;

        /// <summary>
        /// Cả bốn bước phải thao tác trên cùng một kỳ. Khi đổi kỳ, mọi lựa chọn
        /// nguồn cung và preview cũ bị xóa để không thể chốt nhầm snapshot.
        /// </summary>
        public void SetPeriod(int year, int month)
        {
            if (Year == year && Month == month)
            {
                return;
            }

            Year = year;
            Month = month;
            ResetSettlementSelection(notify: false);
            Changed?.Invoke();
        }

        public void SetPreview(SettlementPreviewResDTO? preview)
        {
            if (preview is not null && HasPeriod
                && (preview.Year != Year || preview.Month != Month))
            {
                throw new InvalidOperationException("Bản xem trước không thuộc kỳ đang vận hành.");
            }

            Preview = preview;
            if (preview is not null)
            {
                IdempotencyKey ??= $"{preview.Year:D4}{preview.Month:D2}-{Guid.NewGuid():N}";
            }

            Changed?.Invoke();
        }

        public void Reset()
        {
            Year = 0;
            Month = 0;
            ResetSettlementSelection(notify: false);
            Changed?.Invoke();
        }

        public void ResetSettlementSelection() => ResetSettlementSelection(notify: true);

        private void ResetSettlementSelection(bool notify)
        {
            Preview = null;
            Exceptions.Clear();
            IdempotencyKey = null;
            SelectedSupplierId = null;
            if (notify)
            {
                Changed?.Invoke();
            }
        }

        /// <summary>Sau khi chốt/hiệu chỉnh thành công, khóa idempotency phải đổi cho lần sau.</summary>
        public void CompleteConfirmation()
        {
            IdempotencyKey = null;
            Changed?.Invoke();
        }
    }
}
