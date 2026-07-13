using gtas_vpp_be.Authorization;
using gtas_vpp_be.Model.Auth;
using gtas_vpp_be.Model.Library;
using gtas_vpp_shared.DTOs.Req.Permission;
using gtas_vpp_shared.DTOs.Res.Permission;
using gtas_vpp_shared.DTOs.Res.Library;
using gtas_vpp_shared.DTOs.Res;
using gtas_vpp_shared.Constants;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Services;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Linq.Expressions;
using System.Linq.Dynamic.Core;
using static gtas_vpp_be.Service.Helpers.Config;
using PermissionPageDto = gtas_vpp_shared.DTOs.Res.Auth.sp_Authen_Permission_GetPageWithComponentByGroupId;
using PermissionComponentDto = gtas_vpp_shared.DTOs.Res.Auth.sp_Authen_Permission_GetPageWithComponentByGroupId_List_Component;
using AuthGroupDto = gtas_vpp_shared.DTOs.Res.Auth.P02_GroupResDTO;
using UserListDto = gtas_vpp_shared.DTOs.Res.Auth.sp_Authentication_TabUser_UserList;

namespace gtas_vpp_be.Controllers
{
    [ApiController]
    [Authorize(Policy = Permissions.PermissionView)]
    [Route("api/[controller]")]
    public class PermissionController : ControllerBase
    {
        private readonly IGenericRepository<P02_Group> _groupRepository;
        private readonly IGenericRepository<P06_GroupPageComponentMapping> _groupPageComponentMappingRepository;
        private readonly IGenericRepository<P04_UserGroup> _userGroupRepository;
        private readonly IUserNameResolver _userNameResolver;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IPermissionChangeNotifier _permissionChangeNotifier;

        public PermissionController(
            IGenericRepository<P02_Group> groupRepository,
            IGenericRepository<P06_GroupPageComponentMapping> groupPageComponentMappingRepository,
            IGenericRepository<P04_UserGroup> userGroupRepository,
            IUserNameResolver userNameResolver,
            IUnitOfWork unitOfWork,
            IDateTimeProvider dateTimeProvider,
            IPermissionChangeNotifier permissionChangeNotifier)
        {
            _groupRepository = groupRepository;
            _groupPageComponentMappingRepository = groupPageComponentMappingRepository;
            _userGroupRepository = userGroupRepository;
            _userNameResolver = userNameResolver;
            _unitOfWork = unitOfWork;
            // P5/timezone: always pin audit timestamps to Asia/Ho_Chi_Minh, regardless
            // of host timezone or client-supplied values.
            _dateTimeProvider = dateTimeProvider;
            _permissionChangeNotifier = permissionChangeNotifier;
        }

        private int CurrentUserId => int.TryParse(User.FindFirst("UserID")?.Value, out var id) ? id : 0;

        [HttpGet("groups")]
        public async Task<IActionResult> GetGroups(
            [FromQuery] bool getFullName = true,
            [FromQuery] string? filter = null,
            [FromQuery] int? skip = null,
            [FromQuery] int? top = null,
            [FromQuery] string? orderby = null,
            [FromQuery] string? distinct = null,
            [FromQuery] string? distinctFilter = null)
        {
            IQueryable<P02_GroupResDTO> query = _unitOfWork.VPPContext.Set<P02_Group>()
                .AsNoTracking()
                .Select(group => new P02_GroupResDTO
                {
                    Id = group.Id,
                    Description = group.Description,
                    CreateUserId = group.CreateUserId,
                    CreateDate = group.CreateDate,
                    UpdateUserId = group.UpdateUserId,
                    UpdateDate = group.UpdateDate,
                    IsDeleted = group.IsDeleted,
                    MemberCompanyCode = 0,
                    GroupName = group.GroupName,
                    ParentGroupId = group.ParentGroupId
                });

            if (!string.IsNullOrWhiteSpace(filter))
            {
                try
                {
                    query = query.Where(filter);
                }
                catch
                {
                    // Keep backward compatibility if Radzen sends an unsupported expression.
                }
            }

            if (!string.IsNullOrWhiteSpace(distinct))
            {
                var propertyInfo = typeof(P02_GroupResDTO).GetProperty(distinct);
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
                        var dto = new P02_GroupResDTO();
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
        public async Task<IActionResult> GetGroupById(Guid id, [FromQuery] bool getFullName = true)
        {
            var entity = await GetByIdAsync(_groupRepository, id, getFullName);
            if (entity is null) return Ok(null);

            var rs = entity.Adapt<P02_GroupResDTO>();
            return Ok(rs);
        }

        [HttpGet("groups/{id:guid}/page-components")]
        public async Task<IActionResult> GetGroupPageComponents(Guid id, [FromQuery] bool? showDeleted = false)
        {
            var query = _unitOfWork.VPPContext.Set<P06_GroupPageComponentMapping>()
                .AsNoTracking()
                .Include(x => x.P05_PageComponentMapping)!.ThenInclude(x => x!.P01_Page)
                .Include(x => x.P05_PageComponentMapping)!.ThenInclude(x => x!.P03_Component)
                .Where(x => x.P02_GroupId == id
                            && x.P05_PageComponentMapping != null
                            && x.P05_PageComponentMapping.P01_Page != null
                            && x.P05_PageComponentMapping.P03_Component != null);

            if (showDeleted != true)
            {
                query = query.Where(x => !x.P05_PageComponentMapping!.P01_Page!.IsDeleted
                                         && !x.P05_PageComponentMapping!.P03_Component!.IsDeleted);
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
                .GroupBy(x => x.P05_PageComponentMapping!.P01_PageId)
                .OrderBy(x => x.First().P05_PageComponentMapping!.P01_Page!.PageCode)
                .Select(pageGroup =>
                {
                    var page = pageGroup.First().P05_PageComponentMapping!.P01_Page!;

                    return new PermissionPageDto
                    {
                        GroupId = id,
                        PageId = page.Id,
                        PageCode = page.PageCode,
                        PageName = page.PageName,
                        Description = page.Description,
                        CreateUserId = page.CreateUserId,
                        CreateDate = page.CreateDate,
                        UpdateUserId = page.UpdateUserId,
                        UpdateDate = page.UpdateDate,
                        IsDeleted = page.IsDeleted,
                        List_Component = pageGroup
                            .OrderBy(x => x.P05_PageComponentMapping!.P03_Component!.ComponentName)
                            .Select(groupMapping =>
                            {
                                var pageComponentMapping = groupMapping.P05_PageComponentMapping!;
                                var component = pageComponentMapping.P03_Component!;
                                companyLookup.TryGetValue(groupMapping.MemberCompanyCode, out var company);

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
                                    IsDeleted = component.IsDeleted
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
        public async Task<IActionResult> UpdateGroup(Guid id, [FromBody] P02_GroupUpdateReqDTO req)
        {
            var current = await GetByIdAsync(_groupRepository, id, true);

            if (current is null)
            {
                return NotFound("Group not found.");
            }

            // Validate circular reference
            if (req.ParentGroupId.HasValue && req.ParentGroupId.Value != Guid.Empty)
            {
                if (await HasCircularReference(id, req.ParentGroupId.Value))
                {
                    return BadRequest(new { 
                        message = "Cannot set parent group: This would create a circular reference in the group hierarchy." 
                    });
                }

                // Validate hierarchy depth (optional - warn if too deep)
                var depth = await GetHierarchyDepth(req.ParentGroupId.Value);
                if (depth >= 10)
                {
                    return BadRequest(new { 
                        message = $"Cannot set parent group: This would create a hierarchy that is too deep (current depth: {depth + 1}). Maximum recommended depth is 10 levels." 
                    });
                }
            }

            req.Adapt(current);
            current.UpdateUserId = CurrentUserId;
            current.UpdateDate = _dateTimeProvider.Now;

            var updated = await _groupRepository.UpdateAsync(current);
            var response = updated.Adapt<P02_GroupResDTO>();

            return Ok(response);
        }

        private async Task<bool> HasCircularReference(Guid groupId, Guid parentId)
        {
            // Check if setting parentId as parent of groupId would create a circular reference
            var visited = new HashSet<Guid> { groupId };
            var currentId = parentId;
            int maxDepth = 50; // Prevent infinite loop in case of data corruption
            int depth = 0;

            while (currentId != Guid.Empty && depth < maxDepth)
            {
                // If we've seen this ID before, we have a circular reference
                if (visited.Contains(currentId))
                {
                    return true;
                }

                visited.Add(currentId);

                // Get the parent of current group
                var parent = await GetByIdAsync(_groupRepository, currentId, false);

                if (parent?.ParentGroupId == null || parent.ParentGroupId == Guid.Empty)
                {
                    break; // Reached the top of hierarchy
                }

                currentId = parent.ParentGroupId.Value;
                depth++;
            }

            return false;
        }

        private async Task<int> GetHierarchyDepth(Guid groupId)
        {
            int depth = 0;
            var currentId = groupId;
            int maxDepth = 50;

            while (currentId != Guid.Empty && depth < maxDepth)
            {
                var group = await GetByIdAsync(_groupRepository, currentId, false);

                if (group?.ParentGroupId == null || group.ParentGroupId == Guid.Empty)
                {
                    break;
                }

                currentId = group.ParentGroupId.Value;
                depth++;
            }

            return depth;
        }

        [HttpPatch("component-mapping")]
        [Authorize(Policy = Permissions.PermissionManage)]
        public async Task<IActionResult> PatchComponentMapping([FromBody] PatchComponentMappingReqDTO req)
        {
            var current = (await ReadAsync(
                    _groupPageComponentMappingRepository,
                    expression: x => x.P05_PageComponentMappingId == req.P05_PageComponentMappingId
                                  && x.P02_GroupId == req.P02_GroupId))
                .FirstOrDefault();

            if (current is null)
            {
                return NotFound("Component mapping not found.");
            }

            var before = new { current.IsVisible, current.IsEnable };
            var componentCode = await _unitOfWork.VPPContext.Set<P05_PageComponentMapping>()
                .AsNoTracking()
                .Where(mapping => mapping.Id == current.P05_PageComponentMappingId)
                .Select(mapping => mapping.P03_Component != null
                    ? mapping.P03_Component.ComponentCode
                    : string.Empty)
                .FirstOrDefaultAsync();

            if ((!req.IsVisible || !req.IsEnable)
                && (string.Equals(componentCode, Permissions.PermissionUser, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(componentCode, Permissions.PermissionComponent, StringComparison.OrdinalIgnoreCase)))
            {
                var otherPermissionManagers = await _unitOfWork.VPPContext
                    .Set<P06_GroupPageComponentMapping>()
                    .AsNoTracking()
                    .CountAsync(mapping =>
                        !(mapping.P05_PageComponentMappingId == current.P05_PageComponentMappingId
                            && mapping.P02_GroupId == current.P02_GroupId)
                        && mapping.IsVisible
                        && mapping.IsEnable
                        && mapping.MemberCompanyCode == current.MemberCompanyCode
                        && mapping.P05_PageComponentMapping != null
                        && mapping.P05_PageComponentMapping.P03_Component != null
                        && (mapping.P05_PageComponentMapping.P03_Component.ComponentCode == Permissions.PermissionUser
                            || mapping.P05_PageComponentMapping.P03_Component.ComponentCode == Permissions.PermissionComponent));

                if (otherPermissionManagers == 0)
                {
                    return Conflict(new { message = "Không thể xóa quyền quản trị phân quyền cuối cùng của công ty." });
                }
            }

            req.Adapt(current);
            current.UpdateUserId = CurrentUserId;
            current.UpdateDate = _dateTimeProvider.Now;

            var rs = await _groupPageComponentMappingRepository.UpdateAsync(
                current,
                new Expression<Func<P06_GroupPageComponentMapping, object>>[]
                {
                    x => x.IsEnable,
                    x => x.IsVisible,
                    x => x.UpdateUserId,
                    x => x.UpdateDate
                });

            Serilog.Log.Information(
                "Permission changed: Actor={ActorUserId}, Group={GroupId}, Company={CompanyCode}, Component={ComponentCode}, Before={@Before}, After={@After}",
                CurrentUserId,
                current.P02_GroupId,
                current.MemberCompanyCode,
                componentCode,
                before,
                new { current.IsVisible, current.IsEnable });

            await _permissionChangeNotifier.NotifyGroupChangedAsync(current.P02_GroupId, HttpContext.RequestAborted);
            return Ok(rs);
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers(
            [FromQuery] string? search = null,
            [FromQuery] string? filter = null,
            [FromQuery] int? skip = null,
            [FromQuery] int? top = null,
            [FromQuery] string? orderby = null,
            [FromQuery] string? distinct = null,
            [FromQuery] string? distinctFilter = null)
        {
            var hasSearch = !string.IsNullOrWhiteSpace(search);
            var usersQuery = _unitOfWork.VPPContext.v_Users.AsNoTracking();

            if (hasSearch)
            {
                var searchText = search!.Trim();
                usersQuery = usersQuery.Where(x =>
                    (x.UserLogin != null && x.UserLogin.Contains(searchText))
                    || (x.EmailAddress1 != null && x.EmailAddress1.Contains(searchText))
                    || (x.FullName != null && x.FullName.Contains(searchText)));
            }

            var userGroupsQuery = _unitOfWork.VPPContext.Set<P04_UserGroup>()
                .AsNoTracking()
                .Include(x => x.P02_Group)
                .Include(x => x.LEX02_CompanyDepartmentLocation);

            IQueryable<UserListDto> query =
                from user in usersQuery
                join userGroup in userGroupsQuery on user.UserID equals userGroup.UserId into userGroupJoin
                from userGroup in userGroupJoin.DefaultIfEmpty()
                select new UserListDto
                {
                    Id = userGroup == null ? Guid.Empty : userGroup.Id,
                    UserId = user.UserID,
                    UserLogin = user.UserLogin,
                    FullName = user.FullName,
                    Email = user.EmailAddress1,
                    GoogleEmail = user.GoogleEmail,
                    IsAdmin = userGroup != null
                              && userGroup.P02_Group != null
                              && (userGroup.P02_Group.GroupName == "Admin" || userGroup.P02_Group.GroupName == "Administrator"),
                    GroupId = userGroup == null ? Guid.Empty : userGroup.P02_GroupId,
                    GroupName = userGroup == null || userGroup.P02_Group == null ? string.Empty : userGroup.P02_Group.GroupName,
                    CreateUserId = userGroup == null ? 0 : userGroup.CreateUserId,
                    CreateDate = userGroup == null ? null : userGroup.CreateDate,
                    UpdateUserId = userGroup == null ? 0 : userGroup.UpdateUserId,
                    UpdateDate = userGroup == null ? null : userGroup.UpdateDate,
                    IsDeleted = userGroup != null && userGroup.IsDeleted,
                    TypeOfUser = userGroup == null ? "GTAS User" : "Transportation User",
                    Description = userGroup == null ? null : userGroup.Description,
                    DepartmentName = userGroup == null || userGroup.LEX02_CompanyDepartmentLocation == null
                        ? user.DepartmentCode
                        : userGroup.LEX02_CompanyDepartmentLocation.LEX02Name,
                    L05_DepartmentId = userGroup == null ? null : userGroup.LEX02_CompanyDepartmentLocationId,
                    UserGroup = userGroup == null || userGroup.P02_Group == null
                        ? null
                        : new AuthGroupDto
                        {
                            Id = userGroup.P02_Group.Id,
                            GroupName = userGroup.P02_Group.GroupName,
                            ParentGroupId = userGroup.P02_Group.ParentGroupId,
                            Description = userGroup.P02_Group.Description,
                            CreateUserId = userGroup.P02_Group.CreateUserId,
                            CreateDate = userGroup.P02_Group.CreateDate,
                            UpdateUserId = userGroup.P02_Group.UpdateUserId,
                            UpdateDate = userGroup.P02_Group.UpdateDate,
                            IsDeleted = userGroup.P02_Group.IsDeleted
                        }
                };

            if (!string.IsNullOrWhiteSpace(filter))
            {
                try
                {
                    query = query.Where(filter);
                }
                catch
                {
                    // Keep the endpoint resilient to unsupported Radzen expressions.
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

        [HttpDelete("groups/{id:guid}")]
        [Authorize(Policy = Permissions.PermissionManage)]
        public async Task<IActionResult> DeleteGroup(Guid id)
        {
            var activeUsers = await _unitOfWork.VPPContext.Set<P04_UserGroup>()
                .AsNoTracking()
                .AnyAsync(mapping => mapping.P02_GroupId == id && !mapping.IsDeleted);
            if (activeUsers)
            {
                return Conflict(new { message = "Không thể xóa nhóm đang có người dùng hoạt động." });
            }

            var success = await _groupRepository.DeleteAsync(id);

            return Ok(new { success });
        }
        [HttpPost("user-groups")]
        [Authorize(Policy = Permissions.PermissionManage)]
        public async Task<IActionResult> CreateUserGroup([FromBody] P04_UserGroupUpsertReqDTO req)
        {
            var alreadyAssigned = await _unitOfWork.VPPContext.Set<P04_UserGroup>()
                .AsNoTracking()
                .AnyAsync(mapping => mapping.UserId == req.UserId && !mapping.IsDeleted);
            if (alreadyAssigned)
            {
                return Conflict(new { message = "Mỗi người dùng chỉ được có một nhóm quyền đang hoạt động." });
            }

            var entity = req.Adapt<P04_UserGroup>();
            entity.Id = Guid.Empty;
            entity.LEX02_CompanyDepartmentLocationId = req.LEX02_CompanyDepartmentLocationId ?? Guid.Empty;
            var now = _dateTimeProvider.Now;
            entity.CreateDate = now;
            entity.UpdateDate = now;
            entity.CreateUserId = CurrentUserId;
            entity.UpdateUserId = CurrentUserId;

            var created = await _userGroupRepository.AddAsync(entity) ?? entity;
            await _permissionChangeNotifier.NotifyUserChangedAsync(entity.UserId, HttpContext.RequestAborted);
            return Ok(created.Adapt<P04_UserGroupResDTO>());
        }

        [HttpGet("user-groups")]
        public async Task<IActionResult> GetUserGroups([FromQuery] int? userId)
        {
            try
            {
                if (userId.HasValue)
                {
                    var userGroups = await ReadAsync(
                        _userGroupRepository,
                        getFullName: true,
                        expression: x => x.UserId == userId.Value);

                    var dtoList = userGroups?.Adapt<List<gtas_vpp_shared.DTOs.Res.Auth.P04_UserGroupResDTO>>();
                    return Ok(dtoList ?? new List<gtas_vpp_shared.DTOs.Res.Auth.P04_UserGroupResDTO>());
                }
                else
                {
                    var allUserGroups = await ReadAsync(_userGroupRepository, getFullName: true);

                    var dtoList = allUserGroups?.Adapt<List<gtas_vpp_shared.DTOs.Res.Auth.P04_UserGroupResDTO>>();
                    return Ok(dtoList ?? new List<gtas_vpp_shared.DTOs.Res.Auth.P04_UserGroupResDTO>());
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = $"Error retrieving user groups: {ex.Message}" });
            }
        }

        [HttpPut("user-groups/{id:guid}")]
        [Authorize(Policy = Permissions.PermissionManage)]
        public async Task<IActionResult> UpdateUserGroup(Guid id, [FromBody] P04_UserGroupUpsertReqDTO req)
        {
            var current = await GetByIdAsync(_userGroupRepository, id, true);

            if (current is null)
            {
                return NotFound("User group not found.");
            }

            var duplicateAssignment = await _unitOfWork.VPPContext.Set<P04_UserGroup>()
                .AsNoTracking()
                .AnyAsync(mapping => mapping.Id != id
                    && mapping.UserId == req.UserId
                    && !mapping.IsDeleted);
            if (duplicateAssignment)
            {
                return Conflict(new { message = "Mỗi người dùng chỉ được có một nhóm quyền đang hoạt động." });
            }

            var oldGroupId = current.P02_GroupId;

            req.Adapt(current);
            current.LEX02_CompanyDepartmentLocationId = req.LEX02_CompanyDepartmentLocationId ?? Guid.Empty;
            current.UpdateDate = _dateTimeProvider.Now;
            current.UpdateUserId = CurrentUserId;

            var updated = await _userGroupRepository.UpdateAsync(current);
            await Task.WhenAll(
                _permissionChangeNotifier.NotifyGroupChangedAsync(oldGroupId, HttpContext.RequestAborted),
                _permissionChangeNotifier.NotifyGroupChangedAsync(current.P02_GroupId, HttpContext.RequestAborted),
                _permissionChangeNotifier.NotifyUserChangedAsync(current.UserId, HttpContext.RequestAborted));
            return Ok(updated.Adapt<P04_UserGroupResDTO>());
        }

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
