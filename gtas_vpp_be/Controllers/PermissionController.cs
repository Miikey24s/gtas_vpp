using gtas_vpp_be.Model.Auth;
using gtas_vpp_be.Model.Library;
using gtas_vpp_shared.DTOs.Req.Permission;
using gtas_vpp_shared.DTOs.Res.Permission;
using gtas_vpp_shared.DTOs.Res.Library;
using gtas_vpp_be.Service.Services;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using static gtas_vpp_be.Service.Helpers.Config;

namespace ggtas_vpp_be.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class PermissionController : ControllerBase
    {
        private readonly IBussinessService _bussinessService;

        public PermissionController(IBussinessService bussinessService)
        {
            _bussinessService = bussinessService;
        }

        [HttpGet("groups")]
        public async Task<IActionResult> GetGroups([FromQuery] bool getFullName = true)
        {
            var data = await _bussinessService.BaseService<P02_Group>(
                EF_BASEMETHOD.EF_GetTAsync, getFullName) ?? new List<P02_Group>();

            var rs = data.Adapt<List<P02_GroupResDTO>>();
            return Ok(rs);
        }

        [HttpGet("groups/{id:guid}")]
        public async Task<IActionResult> GetGroupById(Guid id, [FromQuery] bool getFullName = true)
        {
            var data = await _bussinessService.BaseService<P02_Group>(
                EF_BASEMETHOD.EF_GetTByIdAsync, getFullName, Param: id);

            var entity = data?.FirstOrDefault();
            if (entity is null) return Ok(null);

            var rs = entity.Adapt<P02_GroupResDTO>();
            return Ok(rs);
        }

        [HttpPut("groups/{id:guid}")]
        public async Task<IActionResult> UpdateGroup(Guid id, [FromBody] P02_GroupUpdateReqDTO req)
        {
            var current = (await _bussinessService.BaseService<P02_Group>(
                EF_BASEMETHOD.EF_GetTByIdAsync, true, Param: id))?.FirstOrDefault();

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
            current.UpdateDate = req.UpdateDate ?? DateTime.Now;

            var rs = await _bussinessService.BaseService<P02_Group>(
                EF_BASEMETHOD.EF_Update,
                objs: new List<P02_Group> { current });

            var updated = rs?.FirstOrDefault() ?? current;
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
                var parent = (await _bussinessService.BaseService<P02_Group>(
                    EF_BASEMETHOD.EF_GetTByIdAsync, false, Param: currentId))?.FirstOrDefault();

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
                var group = (await _bussinessService.BaseService<P02_Group>(
                    EF_BASEMETHOD.EF_GetTByIdAsync, false, Param: currentId))?.FirstOrDefault();

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
            var current = (await _bussinessService.BaseService<P06_GroupPageComponentMapping>(
                EF_BASEMETHOD.EF_GetTAsync,
                expression: x => x.P05_PageComponentMappingId == req.P05_PageComponentMappingId
                              && x.P02_GroupId == req.P02_GroupId))
                ?.FirstOrDefault();

            if (current is null)
            {
                return NotFound("Component mapping not found.");
            }

            req.Adapt(current);
            current.UpdateDate = req.UpdateDate ?? DateTime.Now;

            var rs = await _bussinessService.BaseService<P06_GroupPageComponentMapping>(
                EF_BASEMETHOD.EF_Update,
                objs: new List<P06_GroupPageComponentMapping> { current },
                properties: new Expression<Func<P06_GroupPageComponentMapping, object>>[]
                {
                    x => x.IsEnable,
                    x => x.IsVisible,
                    x => x.UpdateUserId,
                    x => x.UpdateDate
                });

            return Ok(rs?.FirstOrDefault() ?? current);
        }

        [HttpDelete("groups/{id:guid}")]
        public async Task<IActionResult> DeleteGroup(Guid id)
        {
            var rs = await _bussinessService.BaseService<P02_Group>(
                EF_BASEMETHOD.EF_DeleteAsync, Param: id);

            return Ok(new { success = rs is not null });
        }
        [HttpPost("user-groups")]
        public async Task<IActionResult> CreateUserGroup([FromBody] P04_UserGroupUpsertReqDTO req)
        {
            var entity = req.Adapt<P04_UserGroup>();
            entity.Id = Guid.Empty;
            entity.LEX02_CompanyDepartmentLocationId = req.LEX02_CompanyDepartmentLocationId ?? Guid.Empty;
            entity.CreateDate = req.CreateDate ?? DateTime.Now;
            entity.UpdateDate = req.UpdateDate ?? DateTime.Now;

            var rs = await _bussinessService.BaseService<P04_UserGroup>(
                EF_BASEMETHOD.EF_Create,
                objs: new List<P04_UserGroup> { entity });

            var created = rs?.FirstOrDefault() ?? entity;
            return Ok(created.Adapt<P04_UserGroupResDTO>());
        }

        [HttpGet("user-groups")]
        public async Task<IActionResult> GetUserGroups([FromQuery] int? userId)
        {
            try
            {
                if (userId.HasValue)
                {
                    var userGroups = await _bussinessService.BaseService<P04_UserGroup>(
                        EF_BASEMETHOD.EF_GetTAsync,
                        getFullName: true,
                        expression: x => x.UserId == userId.Value);

                    var dtoList = userGroups?.Adapt<List<gtas_vpp_shared.DTOs.Res.Auth.P04_UserGroupResDTO>>();
                    return Ok(dtoList ?? new List<gtas_vpp_shared.DTOs.Res.Auth.P04_UserGroupResDTO>());
                }
                else
                {
                    var allUserGroups = await _bussinessService.BaseService<P04_UserGroup>(
                        EF_BASEMETHOD.EF_GetTAsync,
                        getFullName: true);

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
            var current = (await _bussinessService.BaseService<P04_UserGroup>(
                EF_BASEMETHOD.EF_GetTByIdAsync, true, Param: id))?.FirstOrDefault();

            if (current is null)
            {
                return NotFound("User group not found.");
            }

            req.Adapt(current);
            current.LEX02_CompanyDepartmentLocationId = req.LEX02_CompanyDepartmentLocationId ?? Guid.Empty;
            current.UpdateDate = req.UpdateDate ?? DateTime.Now;

            var rs = await _bussinessService.BaseService<P04_UserGroup>(
                EF_BASEMETHOD.EF_Update,
                objs: new List<P04_UserGroup> { current });

            var updated = rs?.FirstOrDefault() ?? current;
            return Ok(updated.Adapt<P04_UserGroupResDTO>());
        }
    }
}
