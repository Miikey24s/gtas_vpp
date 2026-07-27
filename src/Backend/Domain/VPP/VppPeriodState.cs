namespace gtas_vpp_be.Model.VPP;

/// <summary>
/// Vòng đời được lưu bền vững của kỳ VPP. Các giá trị được khai báo tường minh để
/// database và API có thể phát triển mà không phải suy trạng thái từ dòng yêu cầu.
/// </summary>
public enum VppPeriodState
{
    Open = 0,
    SubmissionClosed = 1,
    Pricing = 2,
    Settled = 3
}
