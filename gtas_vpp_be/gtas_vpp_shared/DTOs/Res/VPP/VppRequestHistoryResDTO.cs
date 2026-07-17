namespace gtas_vpp_shared.DTOs.Res.VPP
{
    public sealed class VppRequestHistoryResDTO
    {
        public Guid RequestSeriesId { get; set; }
        public Guid CurrentRequestId { get; set; }
        public IReadOnlyList<VppRequestResDTO> Revisions { get; set; }
            = Array.Empty<VppRequestResDTO>();
        public IReadOnlyList<VppRequestTimelineEventResDTO> Timeline { get; set; }
            = Array.Empty<VppRequestTimelineEventResDTO>();
    }

    public sealed class VppRequestTimelineEventResDTO
    {
        public Guid Id { get; set; }
        public Guid RequestId { get; set; }
        public string Action { get; set; } = string.Empty;
        public DateTime OccurredAt { get; set; }
        public int? ActorUserId { get; set; }
        public string? ActorName { get; set; }
        public int? RevisionNumber { get; set; }
        public string? Reason { get; set; }
    }
}
