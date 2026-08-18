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
        private readonly ISecurityAuditQueryService _securityAuditQueryService;
        private readonly IUserAdministrationQueryService _userAdministrationQueryService;

        public PermissionController(
            IGenericRepository<PermissionGroup> groupRepository,
            IGenericRepository<GroupPageComponentMapping> groupPageComponentMappingRepository,
            IGenericRepository<UserGroupMembership> userGroupRepository,
            IUserNameResolver userNameResolver,
            IUnitOfWork unitOfWork,
            IDateTimeProvider dateTimeProvider,
            IPermissionChangeNotifier permissionChangeNotifier,
            IMembershipAdministrationService membershipAdministrationService,
            ISecurityAuditQueryService securityAuditQueryService,
            IUserAdministrationQueryService userAdministrationQueryService)
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
            _securityAuditQueryService = securityAuditQueryService;
            _userAdministrationQueryService = userAdministrationQueryService;
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
                        : group.UserGroupMemberships
                            .Where(membership => !membership.IsDeleted)
                            .Select(membership => membership.AccountId ?? membership.UserId)
                            .Distinct()
                            .Count(),
                    PermissionCount = group.GroupPageComponentMappings == null
                        ? 0
                        : group.GroupPageComponentMappings.Count(mapping =>
                            mapping.MemberCompanyCode == CanonicalRbac.DefaultMemberCompanyCode
                            && mapping.IsVisible
                            && mapping.IsEnable
                            && mapping.PageComponentMapping != null
                            && mapping.PageComponentMapping.PermissionPage != null
                            && !mapping.PageComponentMapping.PermissionPage.IsDeleted
                            && mapping.PageComponentMapping.PermissionComponent != null
                            && !mapping.PageComponentMapping.PermissionComponent.IsDeleted)
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
            var page = await _securityAuditQueryService.GetPageAsync(
                new SecurityAuditQuery(
                    search,
                    action,
                    outcome,
                    occurredFromUtc,
                    occurredToUtc,
                    skip,
                    top,
                    orderby),
                cancellationToken);

            Response.Headers.Append("X-Total-Count", page.TotalCount.ToString(CultureInfo.InvariantCulture));
            return Ok(page.Items);
        }

        [HttpGet("security-audits/filter-options")]
        [Authorize(Policy = Permissions.PermissionManage)]
        [ProducesResponseType(typeof(SecurityAuditFilterOptionsResDTO), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSecurityAuditFilterOptions(CancellationToken cancellationToken)
            => Ok(await _securityAuditQueryService.GetFilterOptionsAsync(cancellationToken));

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
            var result = await _userAdministrationQueryService.GetPageAsync(
                new UserAdministrationQuery(
                    search,
                    accountStatus,
                    groupId,
                    departmentId,
                    hasActiveMembership,
                    filter,
                    skip,
                    top,
                    orderby,
                    distinct,
                    distinctFilter),
                HttpContext.RequestAborted);

            if (!result.Succeeded)
            {
                return BadRequest(new { code = result.ErrorCode, message = result.ErrorMessage });
            }

            Response.Headers.Append("X-Total-Count", result.TotalCount.ToString(CultureInfo.InvariantCulture));
            return Ok(result.Items);
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
