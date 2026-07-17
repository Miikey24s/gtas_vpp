namespace gtas_vpp_shared.DTOs.Res.VPP
{
    public sealed class VPP_RequestHistoryResDTO
    {
        public Guid RequestSeriesId { get; set; }
        public Guid CurrentRequestId { get; set; }
        public IReadOnlyList<VPP01_RequestHeaderResDTO> Revisions { get; set; }
            = Array.Empty<VPP01_RequestHeaderResDTO>();
        public IReadOnlyList<VPP_RequestTimelineEventResDTO> Timeline { get; set; }
            = Array.Empty<VPP_RequestTimelineEventResDTO>();
    }

    public sealed class VPP_RequestTimelineEventResDTO
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
