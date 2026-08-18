using gtas_vpp_be.Authorization;
using gtas_vpp_be.Model.Auth;
using gtas_vpp_be.Model.Library;
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
using System.Globalization;
using System.Linq.Expressions;
using static gtas_vpp_be.Service.Helpers.Config;
using PermissionPageDto = gtas_vpp_shared.DTOs.Res.Auth.PermissionPageComponentResDTO;
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
        private readonly IGenericRepository<UserGroupMembership> _userGroupRepository;
        private readonly IUserNameResolver _userNameResolver;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMembershipAdministrationService _membershipAdministrationService;
        private readonly IPermissionMappingMutationService _permissionMappingMutationService;
        private readonly ISecurityAuditQueryService _securityAuditQueryService;
        private readonly IUserAdministrationQueryService _userAdministrationQueryService;
        private readonly IPermissionGroupQueryService _permissionGroupQueryService;
        private readonly IPermissionPageComponentQueryService _permissionPageComponentQueryService;

        public PermissionController(
            IGenericRepository<PermissionGroup> groupRepository,
            IGenericRepository<UserGroupMembership> userGroupRepository,
            IUserNameResolver userNameResolver,
            IUnitOfWork unitOfWork,
            IMembershipAdministrationService membershipAdministrationService,
            IPermissionMappingMutationService permissionMappingMutationService,
            ISecurityAuditQueryService securityAuditQueryService,
            IUserAdministrationQueryService userAdministrationQueryService,
            IPermissionGroupQueryService permissionGroupQueryService,
            IPermissionPageComponentQueryService permissionPageComponentQueryService)
        {
            _groupRepository = groupRepository;
            _userGroupRepository = userGroupRepository;
            _userNameResolver = userNameResolver;
            _unitOfWork = unitOfWork;
            _membershipAdministrationService = membershipAdministrationService;
            _permissionMappingMutationService = permissionMappingMutationService;
            _securityAuditQueryService = securityAuditQueryService;
            _userAdministrationQueryService = userAdministrationQueryService;
            _permissionGroupQueryService = permissionGroupQueryService;
            _permissionPageComponentQueryService = permissionPageComponentQueryService;
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
            var result = await _permissionGroupQueryService.GetPageAsync(
                new PermissionGroupQuery(
                    getFullName,
                    filter,
                    skip,
                    top,
                    orderby,
                    distinct,
                    distinctFilter),
                HttpContext.RequestAborted);

            Response.Headers.Append("X-Total-Count", result.TotalCount.ToString(CultureInfo.InvariantCulture));
            return Ok(result.Items);
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
            var result = await _permissionPageComponentQueryService.GetAsync(
                id,
                showDeleted == true,
                HttpContext.RequestAborted);
            if (!result.PersonaExists)
            {
                return NotFound();
            }

            return Ok(result.Items);
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
            var result = await _permissionMappingMutationService.PatchAsync(
                req,
                CurrentUserId,
                HttpContext.RequestAborted);
            return PermissionMappingResult(result);
        }

        [HttpPatch("component-mappings/batch")]
        [Authorize(Policy = Permissions.PermissionManage)]
        [ProducesResponseType(typeof(BatchPatchComponentMappingsResDTO), StatusCodes.Status200OK)]
        public async Task<IActionResult> PatchComponentMappingsBatch(
            [FromBody] BatchPatchComponentMappingsReqDTO req,
            CancellationToken cancellationToken)
        {
            var result = await _permissionMappingMutationService.PatchBatchAsync(
                req,
                CurrentUserId,
                cancellationToken);
            return PermissionMappingResult(result);
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

        private IActionResult PermissionMappingResult(PermissionMappingMutationResult result) =>
            result.Outcome switch
            {
                PermissionMappingMutationOutcome.Success => Ok(result.Payload),
                PermissionMappingMutationOutcome.BadRequest => BadRequest(result.Payload),
                PermissionMappingMutationOutcome.NotFound => NotFound(result.Payload),
                PermissionMappingMutationOutcome.Conflict => Conflict(result.Payload),
                _ => throw new InvalidOperationException("Unsupported permission mapping result.")
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
