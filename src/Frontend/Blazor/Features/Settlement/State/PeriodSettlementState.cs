using gtas_vpp_shared.DTOs.Req.VPP;
using gtas_vpp_shared.DTOs.Res.VPP;

namespace gtas_vpp_fe.Features.Settlement.State;

/// <summary>
/// Giữ snapshot preview và idempotency theo từng circuit để confirm/correction dùng
/// đúng kỳ, input hash và thời điểm báo giá đã được người dùng xem trước.
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
    /// Đổi kỳ phải xóa toàn bộ selection và preview cũ để không thể chốt nhầm snapshot.
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
        if (preview is not null
            && HasPeriod
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

    /// <summary>
    /// Sau khi chốt/hiệu chỉnh thành công, khóa lần gửi kế tiếp cho đến khi
    /// một preview mới tạo idempotency key mới.
    /// </summary>
    public void RequireFreshPreviewForNextSubmission()
    {
        IdempotencyKey = null;
        Changed?.Invoke();
    }
}
