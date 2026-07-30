using gtas_vpp_be.Authorization;
using gtas_vpp_be.Model.Auth;
using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Model.View;
using gtas_vpp_shared.DTOs.Req.Permission;
using gtas_vpp_shared.DTOs.Res.Permission;
using gtas_vpp_shared.DTOs.Res.Library;
using gtas_vpp_shared.Constants;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Services;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq.Expressions;
using System.Linq.Dynamic.Core;
using static gtas_vpp_be.Service.Helpers.Config;
using PermissionPageDto = gtas_vpp_shared.DTOs.Res.Auth.PermissionPageComponentResDTO;
using PermissionComponentDto = gtas_vpp_shared.DTOs.Res.Auth.PermissionComponentAccessResDTO;
using AuthGroupDto = gtas_vpp_shared.DTOs.Res.Auth.PermissionGroupResDTO;
using UserListDto = gtas_vpp_shared.DTOs.Res.Auth.UserAdministrationResDTO;
using MembershipDto = gtas_vpp_shared.DTOs.Res.Permission.MembershipAdministrationResDTO;
using UserMembershipDto = gtas_vpp_shared.DTOs.Res.Auth.UserGroupMembershipResDTO;

namespace gtas_vpp_be.Controllers
{
    [ApiController]
    [Authorize(Policy = Permissions.PermissionView)]
    [Route("api/[controller]")]
    public class PermissionController : ControllerBase
    {
        private readonly IGenericRepository<PermissionGroup> _groupRepository;
        private readonly IGenericRepository<GroupPageComponentMapping> _groupPageComponentMappingRepository;
        private readonly IGenericRepository<UserGroupMembership> _userGroupRepository;
        private readonly IUserNameResolver _userNameResolver;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IPermissionChangeNotifier _permissionChangeNotifier;
        private readonly IMembershipAdministrationService _membershipAdministrationService;

        public PermissionController(
            IGenericRepository<PermissionGroup> groupRepository,
            IGenericRepository<GroupPageComponentMapping> groupPageComponentMappingRepository,
            IGenericRepository<UserGroupMembership> userGroupRepository,
            IUserNameResolver userNameResolver,
            IUnitOfWork unitOfWork,
            IDateTimeProvider dateTimeProvider,
            IPermissionChangeNotifier permissionChangeNotifier,
            IMembershipAdministrationService membershipAdministrationService)
        {
            _groupRepository = groupRepository;
            _groupPageComponentMappingRepository = groupPageComponentMappingRepository;
            _userGroupRepository = userGroupRepository;
            _userNameResolver = userNameResolver;
            _unitOfWork = unitOfWork;
            // P5/timezone: luôn cố định audit timestamp theo Asia/Ho_Chi_Minh, không phụ thuộc
            // múi giờ host hoặc giá trị do client gửi.
            _dateTimeProvider = dateTimeProvider;
            _permissionChangeNotifier = permissionChangeNotifier;
            _membershipAdministrationService = membershipAdministrationService;
        }

        private int CurrentUserId => int.TryParse(User.FindFirst("UserID")?.Value, out var id) ? id : 0;

        [HttpGet("groups")]
        [ProducesResponseType(typeof(List<PermissionGroupResDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetGroups(
            [FromQuery] bool getFullName = true,
            [FromQuery] string? filter = null,
            [FromQuery] int? skip = null,
            [FromQuery] int? top = null,
            [FromQuery] string? orderby = null,
            [FromQuery] string? distinct = null,
            [FromQuery] string? distinctFilter = null)
        {
            var canonicalGroupIds = CanonicalRbac.Personas
                .Select(persona => persona.GroupId)
                .ToArray();

            IQueryable<PermissionGroupResDTO> query = _unitOfWork.VPPContext.Set<PermissionGroup>()
                .AsNoTracking()
                .Where(group => canonicalGroupIds.Contains(group.Id) && !group.IsDeleted)
                .Select(group => new PermissionGroupResDTO
                {
                    Id = group.Id,
                    Description = group.Description,
                    CreatedByUserId = group.CreatedByUserId,
                    CreatedAtUtc = group.CreatedAtUtc,
                    UpdatedByUserId = group.UpdatedByUserId,
                    UpdatedAtUtc = group.UpdatedAtUtc,
                    IsDeleted = group.IsDeleted,
                    MemberCompanyCode = CanonicalRbac.DefaultMemberCompanyCode,
                    GroupCode = group.GroupCode,
                    GroupName = group.GroupName,
                    ParentGroupId = group.ParentGroupId,
                    UserCount = group.UserGroupMemberships == null
                        ? 0
                        : group.UserGroupMemberships.Count(membership => !membership.IsDeleted)
                });

            if (!string.IsNullOrWhiteSpace(filter))
            {
                try
                {
                    query = query.Where(filter);
                }
                catch
                {
                    // Giữ tương thích ngược nếu Radzen gửi biểu thức chưa được hỗ trợ.
                }
            }

            if (!string.IsNullOrWhiteSpace(distinct))
            {
                var propertyInfo = typeof(PermissionGroupResDTO).GetProperty(distinct);
                if (propertyInfo != null)
                {
                    var distinctValues = await query
                        .Select(distinct)
                        .Distinct()
                        .ToDynamicListAsync();

                    distinctValues = distinctValues
                        .Where(val => val != null)
                        .Where(val => string.IsNullOrWhiteSpace(distinctFilter)
                            || (Convert.ToString(val, CultureInfo.CurrentCulture)?.Contains(distinctFilter, StringComparison.OrdinalIgnoreCase) ?? false))
                        .ToList();

                    Response.Headers.Append("X-Total-Count", distinctValues.Count.ToString());

                    IEnumerable<object> pageValues = distinctValues.Cast<object>();
                    if (skip.HasValue && skip.Value > 0)
                    {
                        pageValues = pageValues.Skip(skip.Value);
                    }

                    if (top.HasValue && top.Value > 0)
                    {
                        pageValues = pageValues.Take(top.Value);
                    }

                    var distinctDtos = pageValues.Select(val =>
                    {
                        var dto = new PermissionGroupResDTO();
                        propertyInfo.SetValue(dto, val);
                        return dto;
                    }).ToList();

                    return Ok(distinctDtos);
                }
            }

            var totalCount = await query.CountAsync();

            if (!string.IsNullOrWhiteSpace(orderby))
            {
                try
                {
                    query = query.OrderBy(orderby);
                }
                catch
                {
                    query = query.OrderBy(x => x.IsDeleted).ThenBy(x => x.GroupName);
                }
            }
            else
            {
                query = query.OrderBy(x => x.IsDeleted).ThenBy(x => x.GroupName);
            }

            if (skip.HasValue && skip.Value > 0)
            {
                query = query.Skip(skip.Value);
            }

            if (top.HasValue && top.Value > 0)
            {
                query = query.Take(top.Value);
            }

            Response.Headers.Append("X-Total-Count", totalCount.ToString());

            var page = await query.ToListAsync();
            if (getFullName)
            {
                page = await _userNameResolver.WithUserNamesAsync(page, _unitOfWork.VPPContext);
            }

            return Ok(page);
        }

        [HttpGet("groups/{id:guid}")]
        [ProducesResponseType(typeof(PermissionGroupResDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetGroupById(Guid id, [FromQuery] bool getFullName = true)
        {
            if (!CanonicalRbac.Personas.Any(persona => persona.GroupId == id))
            {
                return NotFound();
            }

            var entity = await GetByIdAsync(_groupRepository, id, getFullName);
            if (entity is null || entity.IsDeleted) return NotFound();

            var rs = entity.Adapt<PermissionGroupResDTO>();
            return Ok(rs);
        }

        [HttpGet("groups/{id:guid}/page-components")]
        [ProducesResponseType(typeof(List<PermissionPageDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetGroupPageComponents(Guid id, [FromQuery] bool? showDeleted = false)
        {
            if (!CanonicalRbac.Personas.Any(persona => persona.GroupId == id))
            {
                return NotFound();
            }

            var query = _unitOfWork.VPPContext.Set<GroupPageComponentMapping>()
                .AsNoTracking()
                .Include(x => x.PageComponentMapping)!.ThenInclude(x => x!.PermissionPage)
                .Include(x => x.PageComponentMapping)!.ThenInclude(x => x!.PermissionComponent)
                .Where(x => x.PermissionGroupId == id
                            && x.MemberCompanyCode == CanonicalRbac.DefaultMemberCompanyCode
                            && x.PageComponentMapping != null
                            && x.PageComponentMapping.PermissionPage != null
                            && x.PageComponentMapping.PermissionComponent != null);

            if (showDeleted != true)
            {
                query = query.Where(x => !x.PageComponentMapping!.PermissionPage!.IsDeleted
                                         && !x.PageComponentMapping!.PermissionComponent!.IsDeleted);
            }

            var groupMappings = await query.ToListAsync();

            if (groupMappings.Count == 0)
            {
                return Ok(new List<PermissionPageDto>());
            }

            var memberCompanyCodes = groupMappings
                .Select(x => x.MemberCompanyCode)
                .Distinct()
                .ToList();

            var companyLookup = memberCompanyCodes.Count == 0
                ? new Dictionary<long, v_WFXCompany>()
                : (await _unitOfWork.VPPContext.v_WFXCompanies
                    .AsNoTracking()
                    .Where(x => memberCompanyCodes.Contains(x.MemberCompanyCode))
                    .ToListAsync())
                    .GroupBy(x => x.MemberCompanyCode)
                    .ToDictionary(x => x.Key, x => x.First());

            var result = groupMappings
                .GroupBy(x => x.PageComponentMapping!.PermissionPageId)
                .OrderBy(x => x.First().PageComponentMapping!.PermissionPage!.PageCode)
                .Select(pageGroup =>
                {
                    var page = pageGroup.First().PageComponentMapping!.PermissionPage!;

                    return new PermissionPageDto
                    {
                        GroupId = id,
                        PageId = page.Id,
                        PageCode = page.PageCode,
                        PageName = page.PageName,
                        Description = page.Description,
                        CreatedByUserId = page.CreatedByUserId,
                        CreatedAtUtc = page.CreatedAtUtc,
                        UpdatedByUserId = page.UpdatedByUserId,
                        UpdatedAtUtc = page.UpdatedAtUtc,
                        IsDeleted = page.IsDeleted,
                        Components = pageGroup
                            .OrderBy(x => x.PageComponentMapping!.PermissionComponent!.ComponentName)
                            .Select(groupMapping =>
                            {
                                var pageComponentMapping = groupMapping.PageComponentMapping!;
                                var component = pageComponentMapping.PermissionComponent!;
                                companyLookup.TryGetValue(groupMapping.MemberCompanyCode, out var company);
                                var isActionGrant = Permissions.IsActionCode(component.ComponentCode);
                                var isProtectedSystemAdminNavigation = id == CanonicalRbac.SystemAdmin.GroupId
                                    && (string.Equals(component.ComponentCode, Permissions.MenuPermission, StringComparison.OrdinalIgnoreCase)
                                        || string.Equals(component.ComponentCode, Permissions.PermissionUser, StringComparison.OrdinalIgnoreCase)
                                        || string.Equals(component.ComponentCode, Permissions.PermissionComponent, StringComparison.OrdinalIgnoreCase));
                                var isInsideRoleCeiling = CanonicalRbac.GetUiComponents(id)
                                    .Contains(component.ComponentCode, StringComparer.OrdinalIgnoreCase);
                                var administrationMode = isActionGrant
                                    ? "ActionMatrix"
                                    : isProtectedSystemAdminNavigation
                                        ? "Required"
                                        : isInsideRoleCeiling
                                            ? "Configurable"
                                            : "OutsideRoleCeiling";

                                return new PermissionComponentDto
                                {
                                    ComponentId = component.Id,
                                    ComponentCode = component.ComponentCode,
                                    ComponentName = component.ComponentName,
                                    Description = component.Description,
                                    IsVisible = groupMapping.IsVisible,
                                    IsEnable = groupMapping.IsEnable,
                                    PageId = page.Id,
                                    GroupId = id,
                                    GroupPageComponentMappingId = pageComponentMapping.Id,
                                    MemberCompanyCode = groupMapping.MemberCompanyCode,
                                    CompanyName = company?.CompanyName,
                                    CompanyShortName = company?.CompanyShortName,
                                    IsDeleted = component.IsDeleted,
                                    IsActionGrant = isActionGrant,
                                    CanConfigure = !component.IsDeleted
                                        && string.Equals(administrationMode, "Configurable", StringComparison.Ordinal),
                                    AdministrationMode = administrationMode
                                };
                            })
                            .ToList()
                    };
                })
                .ToList();

            return Ok(result);
        }

        [HttpPut("groups/{id:guid}")]
        [Authorize(Policy = Permissions.PermissionManage)]
        public async Task<IActionResult> UpdateGroup(Guid id, [FromBody] PermissionGroupUpdateReqDTO req)
        {
            var current = await GetByIdAsync(_groupRepository, id, true);

            if (current is null)
            {
                return NotFound("Group not found.");
            }

            return Conflict(new
            {
                code = "CANONICAL_ROLE_DEFINITION_IMMUTABLE",
                message = "The four flat role definitions are versioned reference data and cannot be edited at runtime."
            });
        }

        [HttpPatch("component-mapping")]
        [Authorize(Policy = Permissions.PermissionManage)]
        public async Task<IActionResult> PatchComponentMapping([FromBody] PatchComponentMappingReqDTO req)
        {
            var current = (await ReadAsync(
                    _groupPageComponentMappingRepository,
                    expression: x => x.PageComponentMappingId == req.PageComponentMappingId
                                  && x.PermissionGroupId == req.PermissionGroupId))
                .FirstOrDefault();

            if (current is null)
            {
                return NotFound("Component mapping not found.");
            }

            var before = new { current.IsVisible, current.IsEnable };
            var componentCode = await _unitOfWork.VPPContext.Set<PageComponentMapping>()
                .AsNoTracking()
                .Where(mapping => mapping.Id == current.PageComponentMappingId)
                .Select(mapping => mapping.PermissionComponent != null
                    ? mapping.PermissionComponent.ComponentCode
                    : string.Empty)
                .FirstOrDefaultAsync();

            if (current.MemberCompanyCode != CanonicalRbac.DefaultMemberCompanyCode
                || !CanonicalRbac.Personas.Any(persona => persona.GroupId == current.PermissionGroupId))
            {
                return Conflict(new
                {
                    code = "NON_CANONICAL_ROLE_MAPPING",
                    message = "Only canonical single-company role mappings can be administered."
                });
            }

            if (Permissions.IsActionCode(componentCode))
            {
                return Conflict(new
                {
                    code = "ACTION_MATRIX_IMMUTABLE",
                    message = "Backend action grants are fixed by the reviewed role matrix."
                });
            }

            if (!CanonicalRbac.GetUiComponents(current.PermissionGroupId)
                    .Contains(componentCode, StringComparer.OrdinalIgnoreCase))
            {
                return Conflict(new
                {
                    code = "COMPONENT_OUTSIDE_ROLE_CEILING",
                    message = "This UI component is outside the canonical role ceiling."
                });
            }

            if (req.IsEnable && !req.IsVisible)
            {
                return BadRequest(new
                {
                    code = "ENABLED_COMPONENT_MUST_BE_VISIBLE",
                    message = "An enabled UI component must also be visible."
                });
            }

            var isProtectedSystemAdminNavigation = current.PermissionGroupId == CanonicalRbac.SystemAdmin.GroupId
                && (string.Equals(componentCode, Permissions.MenuPermission, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(componentCode, Permissions.PermissionUser, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(componentCode, Permissions.PermissionComponent, StringComparison.OrdinalIgnoreCase));
            if (isProtectedSystemAdminNavigation && (!req.IsVisible || !req.IsEnable))
            {
                return Conflict(new
                {
                    code = "SYSTEM_ADMIN_NAVIGATION_REQUIRED",
                    message = "System Admin access-administration navigation cannot be disabled."
                });
            }

            req.Adapt(current);
            current.UpdatedByUserId = CurrentUserId;
            current.UpdatedAtUtc = _dateTimeProvider.Now;

            var rs = await _groupPageComponentMappingRepository.UpdateAsync(
                current,
                new Expression<Func<GroupPageComponentMapping, object>>[]
                {
                    x => x.IsEnable,
                    x => x.IsVisible,
                    x => x.UpdatedByUserId,
                    x => x.UpdatedAtUtc
                });

            Serilog.Log.Information(
                "Permission changed: Actor={ActorUserId}, Group={GroupId}, Company={CompanyCode}, Component={ComponentCode}, Before={@Before}, After={@After}",
                CurrentUserId,
                current.PermissionGroupId,
                current.MemberCompanyCode,
                componentCode,
                before,
                new { current.IsVisible, current.IsEnable });

            await _permissionChangeNotifier.NotifyGroupChangedAsync(current.PermissionGroupId, HttpContext.RequestAborted);
            return Ok(rs);
        }

        [HttpPatch("component-mappings/batch")]
        [Authorize(Policy = Permissions.PermissionManage)]
        [ProducesResponseType(typeof(BatchPatchComponentMappingsResDTO), StatusCodes.Status200OK)]
        public async Task<IActionResult> PatchComponentMappingsBatch(
            [FromBody] BatchPatchComponentMappingsReqDTO req,
            CancellationToken cancellationToken)
        {
            if (req.PermissionGroupId == Guid.Empty || req.Items.Count is 0 or > 500)
            {
                return BadRequest(new
                {
                    code = "INVALID_PERMISSION_BATCH",
                    message = "A canonical group and between 1 and 500 mappings are required."
                });
            }

            if (req.Items.Any(item => item.PageComponentMappingId == Guid.Empty)
                || req.Items.Select(item => item.PageComponentMappingId).Distinct().Count() != req.Items.Count)
            {
                return BadRequest(new
                {
                    code = "DUPLICATE_PERMISSION_MAPPING",
                    message = "Each permission mapping must appear exactly once."
                });
            }

            if (!CanonicalRbac.Personas.Any(persona => persona.GroupId == req.PermissionGroupId))
            {
                return Conflict(new
                {
                    code = "NON_CANONICAL_ROLE_MAPPING",
                    message = "Only canonical single-company role mappings can be administered."
                });
            }

            IDbContextTransaction? transaction = null;
            try
            {
                if (_unitOfWork.VPPContext.Database.IsRelational())
                {
                    transaction = await _unitOfWork.VPPContext.Database.BeginTransactionAsync(
                        IsolationLevel.Serializable,
                        cancellationToken);
                }

                var mappingIds = req.Items.Select(item => item.PageComponentMappingId).ToArray();
                var mappings = await _unitOfWork.VPPContext.Set<GroupPageComponentMapping>()
                    .Include(mapping => mapping.PageComponentMapping)!
                    .ThenInclude(mapping => mapping!.PermissionComponent)
                    .Where(mapping => mapping.PermissionGroupId == req.PermissionGroupId
                                      && mapping.MemberCompanyCode == CanonicalRbac.DefaultMemberCompanyCode
                                      && mappingIds.Contains(mapping.PageComponentMappingId))
                    .ToListAsync(cancellationToken);

                if (mappings.Count != req.Items.Count)
                {
                    return NotFound(new
                    {
                        code = "PERMISSION_MAPPING_NOT_FOUND",
                        message = "One or more permission mappings no longer exist. Reload before saving."
                    });
                }

                var mappingById = mappings.ToDictionary(mapping => mapping.PageComponentMappingId);
                var requestedMappings = new List<(GroupPageComponentMapping Mapping, BatchPatchComponentMappingItemReqDTO Item)>();
                foreach (var item in req.Items)
                {
                    var mapping = mappingById[item.PageComponentMappingId];
                    var componentCode = mapping.PageComponentMapping?.PermissionComponent?.ComponentCode ?? string.Empty;

                    if (Permissions.IsActionCode(componentCode))
                    {
                        return Conflict(new
                        {
                            code = "ACTION_MATRIX_IMMUTABLE",
                            message = "Backend action grants are fixed by the reviewed role matrix."
                        });
                    }

                    if (!CanonicalRbac.GetUiComponents(req.PermissionGroupId)
                            .Contains(componentCode, StringComparer.OrdinalIgnoreCase))
                    {
                        return Conflict(new
                        {
                            code = "COMPONENT_OUTSIDE_ROLE_CEILING",
                            message = "A UI component is outside the canonical role ceiling."
                        });
                    }

                    if (item.IsEnable && !item.IsVisible)
                    {
                        return BadRequest(new
                        {
                            code = "ENABLED_COMPONENT_MUST_BE_VISIBLE",
                            message = "An enabled UI component must also be visible."
                        });
                    }

                    var isProtectedSystemAdminNavigation = req.PermissionGroupId == CanonicalRbac.SystemAdmin.GroupId
                        && (string.Equals(componentCode, Permissions.MenuPermission, StringComparison.OrdinalIgnoreCase)
                            || string.Equals(componentCode, Permissions.PermissionUser, StringComparison.OrdinalIgnoreCase)
                            || string.Equals(componentCode, Permissions.PermissionComponent, StringComparison.OrdinalIgnoreCase));
                    if (isProtectedSystemAdminNavigation && (!item.IsVisible || !item.IsEnable))
                    {
                        return Conflict(new
                        {
                            code = "SYSTEM_ADMIN_NAVIGATION_REQUIRED",
                            message = "System Admin access-administration navigation cannot be disabled."
                        });
                    }

                    requestedMappings.Add((mapping, item));
                }

                var changed = requestedMappings
                    .Where(request => request.Mapping.IsVisible != request.Item.IsVisible
                                      || request.Mapping.IsEnable != request.Item.IsEnable)
                    .ToList();

                foreach (var request in changed)
                {
                    request.Mapping.IsVisible = request.Item.IsVisible;
                    request.Mapping.IsEnable = request.Item.IsEnable;
                    request.Mapping.UpdatedByUserId = CurrentUserId;
                    request.Mapping.UpdatedAtUtc = _dateTimeProvider.Now;
                }

                if (changed.Count > 0)
                {
                    var reason = string.IsNullOrWhiteSpace(req.Reason)
                        ? "Cập nhật quyền giao diện theo lô."
                        : req.Reason.Trim();
                    _unitOfWork.VPPContext.SecurityAudits.Add(new SecurityAudit
                    {
                        Id = Guid.NewGuid(),
                        ActorUserId = CurrentUserId > 0 ? CurrentUserId : null,
                        Action = "PERMISSION_UI_BATCH_UPDATED",
                        ResourceType = "PermissionGroup",
                        ResourceId = req.PermissionGroupId.ToString(),
                        Outcome = "Succeeded",
                        Summary = $"Updated {changed.Count} UI permission mapping(s).",
                        Reason = reason.Length > 500 ? reason[..500] : reason,
                        CorrelationId = Activity.Current?.TraceId.ToString(),
                        OccurredAtUtc = _dateTimeProvider.Now
                    });
                    await _unitOfWork.VPPContext.SaveChangesAsync(cancellationToken);
                }

                if (transaction is not null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }

                if (changed.Count > 0)
                {
                    await _permissionChangeNotifier.NotifyGroupChangedAsync(
                        req.PermissionGroupId,
                        cancellationToken);
                }

                return Ok(new BatchPatchComponentMappingsResDTO
                {
                    PermissionGroupId = req.PermissionGroupId,
                    UpdatedCount = changed.Count
                });
            }
            catch
            {
                if (transaction is not null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }

                throw;
            }
            finally
            {
                if (transaction is not null)
                {
                    await transaction.DisposeAsync();
                }
            }
        }

        [HttpGet("security-audits")]
        [Authorize(Policy = Permissions.PermissionManage)]
        [ProducesResponseType(typeof(List<SecurityAuditResDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSecurityAudits(
            [FromQuery] string? search = null,
            [FromQuery] string? action = null,
            [FromQuery] string? outcome = null,
            [FromQuery] DateTime? occurredFromUtc = null,
            [FromQuery] DateTime? occurredToUtc = null,
            [FromQuery] int? skip = null,
            [FromQuery] int? top = null,
            [FromQuery] string? orderby = null,
            CancellationToken cancellationToken = default)
        {
            var audits = _unitOfWork.VPPContext.SecurityAudits.AsNoTracking();
            if (occurredFromUtc.HasValue)
            {
                audits = audits.Where(audit => audit.OccurredAtUtc >= occurredFromUtc.Value);
            }
            if (occurredToUtc.HasValue)
            {
                audits = audits.Where(audit => audit.OccurredAtUtc <= occurredToUtc.Value);
            }

            var users = _unitOfWork.VPPContext.Users.AsNoTracking();
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

            if (!string.IsNullOrWhiteSpace(action))
            {
                var actionValue = action.Trim();
                query = query.Where(audit => audit.Action == actionValue);
            }
            if (!string.IsNullOrWhiteSpace(outcome))
            {
                var outcomeValue = outcome.Trim();
                query = query.Where(audit => audit.Outcome == outcomeValue);
            }
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchValue = search.Trim();
                query = query.Where(audit =>
                    audit.Action.Contains(searchValue)
                    || audit.ResourceType.Contains(searchValue)
                    || (audit.ResourceId != null && audit.ResourceId.Contains(searchValue))
                    || (audit.Summary != null && audit.Summary.Contains(searchValue))
                    || (audit.Reason != null && audit.Reason.Contains(searchValue))
                    || (audit.CorrelationId != null && audit.CorrelationId.Contains(searchValue))
                    || (audit.ActorUserName != null && audit.ActorUserName.Contains(searchValue))
                    || (audit.ActorFullName != null && audit.ActorFullName.Contains(searchValue))
                    || (audit.TargetUserName != null && audit.TargetUserName.Contains(searchValue))
                    || (audit.TargetFullName != null && audit.TargetFullName.Contains(searchValue)));
            }

            var totalCount = await query.CountAsync(cancellationToken);
            query = OrderSecurityAudits(query, orderby);
            if (skip.GetValueOrDefault() > 0)
            {
                query = query.Skip(skip!.Value);
            }
            query = query.Take(Math.Clamp(top ?? 50, 1, 200));

            Response.Headers.Append("X-Total-Count", totalCount.ToString(CultureInfo.InvariantCulture));
            return Ok(await query.ToListAsync(cancellationToken));
        }

        [HttpGet("security-audits/filter-options")]
        [Authorize(Policy = Permissions.PermissionManage)]
        [ProducesResponseType(typeof(SecurityAuditFilterOptionsResDTO), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSecurityAuditFilterOptions(CancellationToken cancellationToken)
        {
            var actions = await _unitOfWork.VPPContext.SecurityAudits
                .AsNoTracking()
                .Select(audit => audit.Action)
                .Where(value => value != string.Empty)
                .Distinct()
                .OrderBy(value => value)
                .Take(200)
                .ToListAsync(cancellationToken);
            var outcomes = await _unitOfWork.VPPContext.SecurityAudits
                .AsNoTracking()
                .Select(audit => audit.Outcome)
                .Where(value => value != string.Empty)
                .Distinct()
                .OrderBy(value => value)
                .Take(50)
                .ToListAsync(cancellationToken);

            return Ok(new SecurityAuditFilterOptionsResDTO
            {
                Actions = actions,
                Outcomes = outcomes
            });
        }

        [HttpGet("users")]
        [ProducesResponseType(typeof(List<UserListDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetUsers(
            [FromQuery] string? search = null,
            [FromQuery] string? accountStatus = null,
            [FromQuery] Guid? groupId = null,
            [FromQuery] Guid? departmentId = null,
            [FromQuery] bool? hasActiveMembership = null,
            [FromQuery] string? filter = null,
            [FromQuery] int? skip = null,
            [FromQuery] int? top = null,
            [FromQuery] string? orderby = null,
            [FromQuery] string? distinct = null,
            [FromQuery] string? distinctFilter = null)
        {
            var hasSearch = !string.IsNullOrWhiteSpace(search);
            // Identity do ứng dụng sở hữu là nguồn có thẩm quyền cho tài khoản. Dòng lịch sử
            // GTAS_MENU/v_Users không bao giờ được tạo membership có thể ghi.
            var usersQuery = _unitOfWork.VPPContext.Users.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(accountStatus))
            {
                if (!Enum.TryParse<AppAccountStatus>(accountStatus.Trim(), true, out var parsedStatus))
                {
                    return BadRequest(new
                    {
                        code = "INVALID_ACCOUNT_STATUS",
                        message = "Account status must be Active, PendingApproval or Disabled."
                    });
                }

                usersQuery = usersQuery.Where(user => user.AccountStatus == parsedStatus);
            }

            if (hasSearch)
            {
                var searchText = search!.Trim();
                usersQuery = usersQuery.Where(x =>
                    (x.UserName != null && x.UserName.Contains(searchText))
                    || (x.Email != null && x.Email.Contains(searchText))
                    || (x.FullName != null && x.FullName.Contains(searchText)));
            }

            var userGroupsQuery = _unitOfWork.VPPContext.Set<UserGroupMembership>()
                .AsNoTracking()
                .Where(mapping => !mapping.IsDeleted
                    && mapping.AccountId.HasValue
                    && mapping.UserId == mapping.AccountId)
                .Include(x => x.PermissionGroup)
                .Include(x => x.Department);

            IQueryable<UserListDto> query =
                from user in usersQuery
                join userGroup in userGroupsQuery on (int?)user.Id equals userGroup.AccountId into userGroupJoin
                from userGroup in userGroupJoin.DefaultIfEmpty()
                select new UserListDto
                {
                    Id = userGroup == null ? Guid.Empty : userGroup.Id,
                    UserId = user.Id,
                    UserLogin = user.UserName,
                    FullName = user.FullName,
                    Email = user.Email,
                    EmployeeCode = user.EmployeeCode,
                    EmailConfirmed = user.EmailConfirmed,
                    MustChangePassword = user.MustChangePassword,
                    GoogleEmail = null,
                    IsAdmin = userGroup != null
                              && userGroup.PermissionGroup != null
                              && userGroup.PermissionGroup.GroupCode == CanonicalRbac.SystemAdmin.GroupCode,
                    GroupId = userGroup == null ? Guid.Empty : userGroup.PermissionGroupId,
                    GroupName = userGroup == null || userGroup.PermissionGroup == null ? string.Empty : userGroup.PermissionGroup.GroupName,
                    CreatedByUserId = userGroup == null ? 0 : userGroup.CreatedByUserId,
                    CreatedAtUtc = userGroup == null ? null : userGroup.CreatedAtUtc,
                    UpdatedByUserId = userGroup == null ? 0 : userGroup.UpdatedByUserId,
                    UpdatedAtUtc = userGroup == null ? null : userGroup.UpdatedAtUtc,
                    // Các trường tương thích cho Radzen grid hiện tại. UI quản trị chuyên biệt
                    // sẽ hiển thị cả hai trạng thái.
                    IsDeleted = user.AccountStatus != AppAccountStatus.Active || userGroup == null,
                    UserType = user.AccountStatus == AppAccountStatus.Active
                        ? "Active application account"
                        : user.AccountStatus == AppAccountStatus.PendingApproval
                            ? "Pending approval"
                            : "Disabled application account",
                    Description = userGroup == null ? null : userGroup.Description,
                    DepartmentName = userGroup == null || userGroup.Department == null
                         ? string.Empty
                         : userGroup.Department.Name,
                    DepartmentId = userGroup == null ? null : userGroup.DepartmentId,
                    AccountStatus = user.AccountStatus == AppAccountStatus.Active
                         ? nameof(AppAccountStatus.Active)
                         : user.AccountStatus == AppAccountStatus.PendingApproval
                             ? nameof(AppAccountStatus.PendingApproval)
                             : nameof(AppAccountStatus.Disabled),
                    SessionVersion = user.SessionVersion,
                    GroupCode = userGroup == null || userGroup.PermissionGroup == null
                         ? null
                         : userGroup.PermissionGroup.GroupCode,
                    IsActive = user.AccountStatus == AppAccountStatus.Active && userGroup != null,
                    RowVersion = userGroup == null ? null : userGroup.RowVersion,
                    UserGroup = userGroup == null || userGroup.PermissionGroup == null
                        ? null
                        : new AuthGroupDto
                        {
                            Id = userGroup.PermissionGroup.Id,
                            GroupName = userGroup.PermissionGroup.GroupName,
                            ParentGroupId = userGroup.PermissionGroup.ParentGroupId,
                            Description = userGroup.PermissionGroup.Description,
                            CreatedByUserId = userGroup.PermissionGroup.CreatedByUserId,
                            CreatedAtUtc = userGroup.PermissionGroup.CreatedAtUtc,
                            UpdatedByUserId = userGroup.PermissionGroup.UpdatedByUserId,
                            UpdatedAtUtc = userGroup.PermissionGroup.UpdatedAtUtc,
                            IsDeleted = userGroup.PermissionGroup.IsDeleted
                        }
                };

            if (groupId.HasValue)
            {
                query = query.Where(user => user.GroupId == groupId.Value);
            }

            if (departmentId.HasValue)
            {
                query = query.Where(user => user.DepartmentId == departmentId.Value);
            }

            if (hasActiveMembership.HasValue)
            {
                query = hasActiveMembership.Value
                    ? query.Where(user => user.GroupId != Guid.Empty)
                    : query.Where(user => user.GroupId == Guid.Empty);
            }

            if (!string.IsNullOrWhiteSpace(filter))
            {
                try
                {
                    query = query.Where(filter);
                }
                catch
                {
                    // Giữ endpoint hoạt động an toàn khi gặp biểu thức Radzen chưa hỗ trợ.
                }
            }

            if (!string.IsNullOrWhiteSpace(distinct))
            {
                var propertyInfo = typeof(UserListDto).GetProperty(distinct);
                if (propertyInfo != null)
                {
                    var distinctValues = await query
                        .Select(distinct)
                        .Distinct()
                        .ToDynamicListAsync();

                    var filteredValues = distinctValues
                        .Where(val => val != null)
                        .Where(val => string.IsNullOrWhiteSpace(distinctFilter)
                            || (Convert.ToString(val, CultureInfo.CurrentCulture)?.Contains(distinctFilter, StringComparison.OrdinalIgnoreCase) ?? false))
                        .ToList();

                    Response.Headers.Append("X-Total-Count", filteredValues.Count.ToString());

                    IEnumerable<object> pageValues = filteredValues.Cast<object>();
                    if (skip.HasValue && skip.Value > 0)
                    {
                        pageValues = pageValues.Skip(skip.Value);
                    }

                    if (top.HasValue && top.Value > 0)
                    {
                        pageValues = pageValues.Take(top.Value);
                    }

                    var distinctDtos = pageValues.Select(val =>
                    {
                        var dto = new UserListDto();
                        propertyInfo.SetValue(dto, val);
                        return dto;
                    }).ToList();

                    return Ok(distinctDtos);
                }
            }

            var totalCount = await query.CountAsync();

            if (!string.IsNullOrWhiteSpace(orderby))
            {
                try
                {
                    query = query.OrderBy(orderby);
                }
                catch
                {
                    query = query.OrderBy(x => x.IsDeleted).ThenBy(x => x.FullName).ThenBy(x => x.UserLogin);
                }
            }
            else
            {
                query = query.OrderBy(x => x.IsDeleted).ThenBy(x => x.FullName).ThenBy(x => x.UserLogin);
            }

            if (skip.HasValue && skip.Value > 0)
            {
                query = query.Skip(skip.Value);
            }

            if (top.HasValue && top.Value > 0)
            {
                query = query.Take(top.Value);
            }

            var page = await query.ToListAsync();
            page = await _userNameResolver.WithUserNamesAsync(page, _unitOfWork.VPPContext);

            Response.Headers.Append("X-Total-Count", totalCount.ToString());

            return Ok(page);
        }

        [HttpGet("user-groups")]
        [ProducesResponseType(typeof(List<UserMembershipDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetUserGroups([FromQuery] int? userId)
        {
            try
            {
                if (userId.HasValue)
                {
                    var userGroups = await ReadAsync(
                        _userGroupRepository,
                        getFullName: true,
                        expression: x => x.UserId == userId.Value
                                         && x.AccountId == userId.Value
                                         && !x.IsDeleted);

                    var dtoList = userGroups?.Adapt<List<gtas_vpp_shared.DTOs.Res.Auth.UserGroupMembershipResDTO>>();
                    return Ok(dtoList ?? new List<gtas_vpp_shared.DTOs.Res.Auth.UserGroupMembershipResDTO>());
                }
                else
                {
                    var allUserGroups = await ReadAsync(
                        _userGroupRepository,
                        getFullName: true,
                        expression: x => x.AccountId.HasValue
                                         && x.UserId == x.AccountId.Value
                                         && !x.IsDeleted);

                    var dtoList = allUserGroups?.Adapt<List<gtas_vpp_shared.DTOs.Res.Auth.UserGroupMembershipResDTO>>();
                    return Ok(dtoList ?? new List<gtas_vpp_shared.DTOs.Res.Auth.UserGroupMembershipResDTO>());
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = $"Error retrieving user groups: {ex.Message}" });
            }
        }

        [HttpPut("memberships")]
        [Authorize(Policy = Permissions.PermissionManage)]
        [ProducesResponseType(typeof(MembershipDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpsertMembership(
            [FromBody] MembershipUpsertReqDTO command,
            CancellationToken cancellationToken)
        {
            var result = await _membershipAdministrationService.UpsertAsync(
                CurrentUserId,
                command,
                cancellationToken);
            return MembershipResult(result);
        }

        [HttpPost("memberships/deactivate")]
        [Authorize(Policy = Permissions.PermissionManage)]
        [ProducesResponseType(typeof(MembershipDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeactivateMembership(
            [FromBody] MembershipDeactivateReqDTO command,
            CancellationToken cancellationToken)
        {
            var result = await _membershipAdministrationService.DeactivateAsync(
                CurrentUserId,
                command,
                cancellationToken);
            return MembershipResult(result);
        }

        private IActionResult MembershipResult(MembershipAdministrationResult result) =>
            result.Succeeded
                ? Ok(result.Membership)
                : StatusCode(result.StatusCode, new { code = result.Code, message = result.Message });

        private static IQueryable<SecurityAuditResDTO> OrderSecurityAudits(
            IQueryable<SecurityAuditResDTO> query,
            string? orderby) => orderby?.Trim().ToLowerInvariant() switch
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
                "summary asc" => query.OrderBy(audit => audit.Summary).ThenByDescending(audit => audit.OccurredAtUtc),
                "summary desc" => query.OrderByDescending(audit => audit.Summary).ThenByDescending(audit => audit.OccurredAtUtc),
                "reason asc" => query.OrderBy(audit => audit.Reason).ThenByDescending(audit => audit.OccurredAtUtc),
                "reason desc" => query.OrderByDescending(audit => audit.Reason).ThenByDescending(audit => audit.OccurredAtUtc),
                "correlationid asc" => query.OrderBy(audit => audit.CorrelationId).ThenByDescending(audit => audit.OccurredAtUtc),
                "correlationid desc" => query.OrderByDescending(audit => audit.CorrelationId).ThenByDescending(audit => audit.OccurredAtUtc),
                _ => query.OrderByDescending(audit => audit.OccurredAtUtc)
            };

        private async Task<List<T>> ReadAsync<T>(
            IGenericRepository<T> repository,
            bool getFullName = false,
            Expression<Func<T, bool>>? expression = null,
            Func<IQueryable<T>, IQueryable<T>>? include = null) where T : class
        {
            var data = await repository.ReadAsync(expression, include);
            return getFullName
                ? await _userNameResolver.WithUserNamesAsync(data, _unitOfWork.VPPContext)
                : data;
        }

        private async Task<T?> GetByIdAsync<T>(IGenericRepository<T> repository, object id, bool getFullName) where T : class
        {
            var entity = await repository.GetByIdAsync(id);
            if (entity is not null && getFullName)
            {
                await _userNameResolver.IncludeUserInfoAsync(entity, _unitOfWork.VPPContext);
            }

            return entity;
        }
    }
}
