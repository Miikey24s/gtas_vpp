using gtas_vpp_be.Model.Auth;
using gtas_vpp_be.Model.Library;
using gtas_vpp_shared.DTOs.Req.Permission;
using gtas_vpp_shared.DTOs.Res.Permission;
using gtas_vpp_shared.DTOs.Res.Library;
using gtas_vpp_shared.DTOs.Res;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Services;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using static gtas_vpp_be.Service.Helpers.Config;
using PermissionPageDto = gtas_vpp_shared.DTOs.Res.Auth.sp_Authen_Permission_GetPageWithComponentByGroupId;
using PermissionComponentDto = gtas_vpp_shared.DTOs.Res.Auth.sp_Authen_Permission_GetPageWithComponentByGroupId_List_Component;

namespace gtas_vpp_be.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class PermissionController : ControllerBase
    {
        private readonly IGenericRepository<P02_Group> _groupRepository;
        private readonly IGenericRepository<P06_GroupPageComponentMapping> _groupPageComponentMappingRepository;
        private readonly IGenericRepository<P04_UserGroup> _userGroupRepository;
        private readonly IUserNameResolver _userNameResolver;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDateTimeProvider _dateTimeProvider;

        public PermissionController(
            IGenericRepository<P02_Group> groupRepository,
            IGenericRepository<P06_GroupPageComponentMapping> groupPageComponentMappingRepository,
            IGenericRepository<P04_UserGroup> userGroupRepository,
            IUserNameResolver userNameResolver,
            IUnitOfWork unitOfWork,
            IDateTimeProvider dateTimeProvider)
        {
            _groupRepository = groupRepository;
            _groupPageComponentMappingRepository = groupPageComponentMappingRepository;
            _userGroupRepository = userGroupRepository;
            _userNameResolver = userNameResolver;
            _unitOfWork = unitOfWork;
            // P5/timezone: always pin audit timestamps to Asia/Ho_Chi_Minh, regardless
            // of host timezone or client-supplied values.
            _dateTimeProvider = dateTimeProvider;
        }

        [HttpGet("groups")]
        public async Task<IActionResult> GetGroups([FromQuery] bool getFullName = true)
        {
            var data = await ReadAsync(_groupRepository, getFullName);

            var rs = data.Adapt<List<P02_GroupResDTO>>();
            return Ok(rs);
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

            req.Adapt(current);
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

            return Ok(rs);
        }

        [HttpDelete("groups/{id:guid}")]
        public async Task<IActionResult> DeleteGroup(Guid id)
        {
            var success = await _groupRepository.DeleteAsync(id);

            return Ok(new { success });
        }
        [HttpPost("user-groups")]
        public async Task<IActionResult> CreateUserGroup([FromBody] P04_UserGroupUpsertReqDTO req)
        {
            var entity = req.Adapt<P04_UserGroup>();
            entity.Id = Guid.Empty;
            entity.LEX02_CompanyDepartmentLocationId = req.LEX02_CompanyDepartmentLocationId ?? Guid.Empty;
            var now = _dateTimeProvider.Now;
            entity.CreateDate = now;
            entity.UpdateDate = now;

            var created = await _userGroupRepository.AddAsync(entity) ?? entity;
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
        public async Task<IActionResult> UpdateUserGroup(Guid id, [FromBody] P04_UserGroupUpsertReqDTO req)
        {
            var current = await GetByIdAsync(_userGroupRepository, id, true);

            if (current is null)
            {
                return NotFound("User group not found.");
            }

            req.Adapt(current);
            current.LEX02_CompanyDepartmentLocationId = req.LEX02_CompanyDepartmentLocationId ?? Guid.Empty;
            current.UpdateDate = _dateTimeProvider.Now;

            var updated = await _userGroupRepository.UpdateAsync(current);
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
