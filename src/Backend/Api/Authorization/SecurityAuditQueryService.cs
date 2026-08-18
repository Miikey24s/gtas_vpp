using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Helpers.Context;
using gtas_vpp_shared.DTOs.Res.Permission;
using Microsoft.EntityFrameworkCore;

namespace gtas_vpp_be.Authorization;

public sealed record SecurityAuditQuery(
    string? Search,
    string? Action,
    string? Outcome,
    DateTime? OccurredFromUtc,
    DateTime? OccurredToUtc,
    int? Skip,
    int? Top,
    string? OrderBy);

public sealed record SecurityAuditPageResult(
    IReadOnlyList<SecurityAuditResDTO> Items,
    int TotalCount);

public interface ISecurityAuditQueryService
{
    Task<SecurityAuditPageResult> GetPageAsync(
        SecurityAuditQuery request,
        CancellationToken cancellationToken = default);

    Task<SecurityAuditFilterOptionsResDTO> GetFilterOptionsAsync(
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Sở hữu read path của nhật ký bảo mật. Controller chỉ chuyển tham số HTTP và
/// total-count header; join người dùng, filter, sort và paging được giữ tại đây.
/// </summary>
public sealed class SecurityAuditQueryService(VPPContext context) : ISecurityAuditQueryService
{
    private readonly VPPContext _context = context;

    public async Task<SecurityAuditPageResult> GetPageAsync(
        SecurityAuditQuery request,
        CancellationToken cancellationToken = default)
    {
        var audits = _context.SecurityAudits.AsNoTracking();
        if (request.OccurredFromUtc.HasValue)
        {
            audits = audits.Where(audit => audit.OccurredAtUtc >= request.OccurredFromUtc.Value);
        }

        if (request.OccurredToUtc.HasValue)
        {
            audits = audits.Where(audit => audit.OccurredAtUtc <= request.OccurredToUtc.Value);
        }

        // Left join giữ được audit lịch sử kể cả khi actor hoặc target đã bị xóa khỏi bảng tài khoản.
        var users = _context.Users.AsNoTracking();
        IQueryable<SecurityAuditResDTO> query =
            from audit in audits
            join actor in users on audit.ActorUserId equals (int?)actor.Id into actorJoin
            from actor in actorJoin.DefaultIfEmpty()
            join target in users on audit.TargetUserId equals (int?)target.Id into targetJoin
            from target in targetJoin.DefaultIfEmpty()
            select new SecurityAuditResDTO
            {
                Id = audit.Id,
                OccurredAtUtc = audit.OccurredAtUtc,
                ActorUserId = audit.ActorUserId,
                ActorUserName = actor == null ? null : actor.UserName,
                ActorFullName = actor == null ? null : actor.FullName,
                TargetUserId = audit.TargetUserId,
                TargetUserName = target == null ? null : target.UserName,
                TargetFullName = target == null ? null : target.FullName,
                Action = audit.Action,
                ResourceType = audit.ResourceType,
                ResourceId = audit.ResourceId,
                Outcome = audit.Outcome,
                Summary = audit.Summary,
                Reason = audit.Reason,
                CorrelationId = audit.CorrelationId
            };

        query = ApplyFilters(query, request);
        var totalCount = await query.CountAsync(cancellationToken);
        query = Order(query, request.OrderBy);

        if (request.Skip.GetValueOrDefault() > 0)
        {
            query = query.Skip(request.Skip!.Value);
        }

        // Giới hạn cứng bảo vệ endpoint audit khỏi một truy vấn vô tình tải toàn bộ lịch sử.
        query = query.Take(Math.Clamp(request.Top ?? 50, 1, 200));
        var items = await query.ToListAsync(cancellationToken);
        return new SecurityAuditPageResult(items, totalCount);
    }

    public async Task<SecurityAuditFilterOptionsResDTO> GetFilterOptionsAsync(
        CancellationToken cancellationToken = default)
    {
        var actions = await _context.SecurityAudits
            .AsNoTracking()
            .Select(audit => audit.Action)
            .Where(value => value != string.Empty)
            .Distinct()
            .OrderBy(value => value)
            .Take(200)
            .ToListAsync(cancellationToken);
        var outcomes = await _context.SecurityAudits
            .AsNoTracking()
            .Select(audit => audit.Outcome)
            .Where(value => value != string.Empty)
            .Distinct()
            .OrderBy(value => value)
            .Take(50)
            .ToListAsync(cancellationToken);

        return new SecurityAuditFilterOptionsResDTO
        {
            Actions = actions,
            Outcomes = outcomes
        };
    }

    private IQueryable<SecurityAuditResDTO> ApplyFilters(
        IQueryable<SecurityAuditResDTO> query,
        SecurityAuditQuery request)
    {
        if (!string.IsNullOrWhiteSpace(request.Action))
        {
            var action = request.Action.Trim();
            query = query.Where(audit => audit.Action == action);
        }

        if (!string.IsNullOrWhiteSpace(request.Outcome))
        {
            var outcome = request.Outcome.Trim();
            query = query.Where(audit => audit.Outcome == outcome);
        }

        if (string.IsNullOrWhiteSpace(request.Search))
        {
            return query;
        }

        var searchTerms = VietnameseSearch.Tokenize(request.Search);
        if (_context.Database.IsSqlServer())
        {
            foreach (var term in searchTerms)
            {
                var pattern = VietnameseSearch.BuildContainsPattern(term);
                query = query.Where(audit =>
                    EF.Functions.Like(EF.Functions.Collate(audit.Action.Replace("đ", "d").Replace("Đ", "D"), VietnameseSearch.SqlServerCollation), pattern, "\\")
                    || EF.Functions.Like(EF.Functions.Collate(audit.Action.Replace(" ", "").Replace("đ", "d").Replace("Đ", "D"), VietnameseSearch.SqlServerCollation), pattern, "\\")
                    || EF.Functions.Like(EF.Functions.Collate(audit.ResourceType.Replace("đ", "d").Replace("Đ", "D"), VietnameseSearch.SqlServerCollation), pattern, "\\")
                    || EF.Functions.Like(EF.Functions.Collate(audit.ResourceType.Replace(" ", "").Replace("đ", "d").Replace("Đ", "D"), VietnameseSearch.SqlServerCollation), pattern, "\\")
                    || (audit.ResourceId != null && EF.Functions.Like(EF.Functions.Collate(audit.ResourceId.Replace("đ", "d").Replace("Đ", "D"), VietnameseSearch.SqlServerCollation), pattern, "\\"))
                    || (audit.ResourceId != null && EF.Functions.Like(EF.Functions.Collate(audit.ResourceId.Replace(" ", "").Replace("đ", "d").Replace("Đ", "D"), VietnameseSearch.SqlServerCollation), pattern, "\\"))
                    || (audit.Summary != null && EF.Functions.Like(EF.Functions.Collate(audit.Summary.Replace("đ", "d").Replace("Đ", "D"), VietnameseSearch.SqlServerCollation), pattern, "\\"))
                    || (audit.Summary != null && EF.Functions.Like(EF.Functions.Collate(audit.Summary.Replace(" ", "").Replace("đ", "d").Replace("Đ", "D"), VietnameseSearch.SqlServerCollation), pattern, "\\"))
                    || (audit.Reason != null && EF.Functions.Like(EF.Functions.Collate(audit.Reason.Replace("đ", "d").Replace("Đ", "D"), VietnameseSearch.SqlServerCollation), pattern, "\\"))
                    || (audit.Reason != null && EF.Functions.Like(EF.Functions.Collate(audit.Reason.Replace(" ", "").Replace("đ", "d").Replace("Đ", "D"), VietnameseSearch.SqlServerCollation), pattern, "\\"))
                    || (audit.ActorUserName != null && EF.Functions.Like(EF.Functions.Collate(audit.ActorUserName.Replace("đ", "d").Replace("Đ", "D"), VietnameseSearch.SqlServerCollation), pattern, "\\"))
                    || (audit.ActorUserName != null && EF.Functions.Like(EF.Functions.Collate(audit.ActorUserName.Replace(" ", "").Replace("đ", "d").Replace("Đ", "D"), VietnameseSearch.SqlServerCollation), pattern, "\\"))
                    || (audit.ActorFullName != null && EF.Functions.Like(EF.Functions.Collate(audit.ActorFullName.Replace("đ", "d").Replace("Đ", "D"), VietnameseSearch.SqlServerCollation), pattern, "\\"))
                    || (audit.ActorFullName != null && EF.Functions.Like(EF.Functions.Collate(audit.ActorFullName.Replace(" ", "").Replace("đ", "d").Replace("Đ", "D"), VietnameseSearch.SqlServerCollation), pattern, "\\"))
                    || (audit.TargetUserName != null && EF.Functions.Like(EF.Functions.Collate(audit.TargetUserName.Replace("đ", "d").Replace("Đ", "D"), VietnameseSearch.SqlServerCollation), pattern, "\\"))
                    || (audit.TargetUserName != null && EF.Functions.Like(EF.Functions.Collate(audit.TargetUserName.Replace(" ", "").Replace("đ", "d").Replace("Đ", "D"), VietnameseSearch.SqlServerCollation), pattern, "\\"))
                    || (audit.TargetFullName != null && EF.Functions.Like(EF.Functions.Collate(audit.TargetFullName.Replace("đ", "d").Replace("Đ", "D"), VietnameseSearch.SqlServerCollation), pattern, "\\"))
                    || (audit.TargetFullName != null && EF.Functions.Like(EF.Functions.Collate(audit.TargetFullName.Replace(" ", "").Replace("đ", "d").Replace("Đ", "D"), VietnameseSearch.SqlServerCollation), pattern, "\\")));
            }

            return query;
        }

        foreach (var term in searchTerms)
        {
            query = query.Where(audit =>
                VietnameseSearch.Normalize(audit.Action).Contains(term)
                || VietnameseSearch.Compact(audit.Action).Contains(term)
                || VietnameseSearch.Normalize(audit.ResourceType).Contains(term)
                || VietnameseSearch.Compact(audit.ResourceType).Contains(term)
                || VietnameseSearch.Normalize(audit.ResourceId).Contains(term)
                || VietnameseSearch.Compact(audit.ResourceId).Contains(term)
                || VietnameseSearch.Normalize(audit.Summary).Contains(term)
                || VietnameseSearch.Compact(audit.Summary).Contains(term)
                || VietnameseSearch.Normalize(audit.Reason).Contains(term)
                || VietnameseSearch.Compact(audit.Reason).Contains(term)
                || VietnameseSearch.Normalize(audit.ActorUserName).Contains(term)
                || VietnameseSearch.Compact(audit.ActorUserName).Contains(term)
                || VietnameseSearch.Compact(audit.ActorFullName).Contains(term)
                || VietnameseSearch.Normalize(audit.ActorFullName).Contains(term)
                || VietnameseSearch.Normalize(audit.TargetUserName).Contains(term)
                || VietnameseSearch.Compact(audit.TargetUserName).Contains(term)
                || VietnameseSearch.Compact(audit.TargetFullName).Contains(term)
                || VietnameseSearch.Normalize(audit.TargetFullName).Contains(term));
        }

        return query;
    }

    private static IQueryable<SecurityAuditResDTO> Order(
        IQueryable<SecurityAuditResDTO> query,
        string? orderBy) => orderBy?.Trim().ToLowerInvariant() switch
        {
            "occurredatutc asc" => query.OrderBy(audit => audit.OccurredAtUtc),
            "action asc" => query.OrderBy(audit => audit.Action).ThenByDescending(audit => audit.OccurredAtUtc),
            "action desc" => query.OrderByDescending(audit => audit.Action).ThenByDescending(audit => audit.OccurredAtUtc),
            "actorfullname asc" => query.OrderBy(audit => audit.ActorFullName).ThenByDescending(audit => audit.OccurredAtUtc),
            "actorfullname desc" => query.OrderByDescending(audit => audit.ActorFullName).ThenByDescending(audit => audit.OccurredAtUtc),
            "outcome asc" => query.OrderBy(audit => audit.Outcome).ThenByDescending(audit => audit.OccurredAtUtc),
            "outcome desc" => query.OrderByDescending(audit => audit.Outcome).ThenByDescending(audit => audit.OccurredAtUtc),
            "targetfullname asc" => query.OrderBy(audit => audit.TargetFullName).ThenByDescending(audit => audit.OccurredAtUtc),
            "targetfullname desc" => query.OrderByDescending(audit => audit.TargetFullName).ThenByDescending(audit => audit.OccurredAtUtc),
            "resourcetype asc" => query.OrderBy(audit => audit.ResourceType).ThenByDescending(audit => audit.OccurredAtUtc),
            "resourcetype desc" => query.OrderByDescending(audit => audit.ResourceType).ThenByDescending(audit => audit.OccurredAtUtc),
            "resourceid asc" => query.OrderBy(audit => audit.ResourceId).ThenByDescending(audit => audit.OccurredAtUtc),
            "resourceid desc" => query.OrderByDescending(audit => audit.ResourceId).ThenByDescending(audit => audit.OccurredAtUtc),
            "summary asc" => query.OrderBy(audit => audit.Summary).ThenByDescending(audit => audit.OccurredAtUtc),
            "summary desc" => query.OrderByDescending(audit => audit.Summary).ThenByDescending(audit => audit.OccurredAtUtc),
            "reason asc" => query.OrderBy(audit => audit.Reason).ThenByDescending(audit => audit.OccurredAtUtc),
            "reason desc" => query.OrderByDescending(audit => audit.Reason).ThenByDescending(audit => audit.OccurredAtUtc),
            "correlationid asc" => query.OrderBy(audit => audit.CorrelationId).ThenByDescending(audit => audit.OccurredAtUtc),
            "correlationid desc" => query.OrderByDescending(audit => audit.CorrelationId).ThenByDescending(audit => audit.OccurredAtUtc),
            _ => query.OrderByDescending(audit => audit.OccurredAtUtc)
        };
}
