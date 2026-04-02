using gtas_vpp_be.Model;
using gtas_vpp_be.Model.View;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace gtas_vpp_be.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        //private readonly IJwtTokenService _jwtTokenService;
        private readonly VPPMigrationDbContext _authDb;
        private readonly IBussinessService _bussinessService;
        public AuthController(VPPMigrationDbContext authDb, IBussinessService bussinessService)
        {
            _authDb = authDb;
            _bussinessService = bussinessService;
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest();

            //var user1 = await _eFService_Authen.BaseService<v_Users>(Config.EF_BASEMETHOD.EF_GetTAsync,null,null,null
            //                        , x=>x.UserLogin == request.Username && x.PasswordChar == PasswordHelpers.Encrypt(request.Password, true));

            var user = await _authDb.Set<v_Users>()
                                .AsNoTracking()
                                .FirstOrDefaultAsync(u => u.UserLogin == request.Username);

            if (user == null || string.IsNullOrWhiteSpace(user.PasswordChar))
                return Unauthorized();

            string decrypted;
            try
            {
                // PasswordHelpers.Decrypt expects base64 cipher; use hashing = true as implementation uses MD5 on key
                decrypted = PasswordHelpers.Decrypt(user.PasswordChar, useHashing: true);
            }
            catch
            {
                // nếu giải mã lỗi => không hợp lệ
                return Unauthorized();
            }

            if (decrypted != request.Password)
                return Unauthorized();

            // Lấy danh sách group của user (P04_UserGroup.UserId)
            var groupIds = await _authDb.P04_UserGroups
                                         .AsNoTracking()
                                         .Where(ug => ug.UserId == user.UserID)
                                         .Select(ug => ug.P02_GroupId)
                                         .Distinct()
                                         .ToListAsync();

            // Lấy mapping P06 cho các group này (kèm P05 -> P01/P03 để biết page/component)
            var mappings = await _authDb.P06_GroupPageComponentMappings
                                       .AsNoTracking()
                                       .Include(m => m.P05_PageComponentMapping)
                                           .ThenInclude(pm => pm!.P01_Page)
                                       .Include(m => m.P05_PageComponentMapping)
                                           .ThenInclude(pm => pm!.P03_Component)
                                       .Where(m => groupIds.Contains(m.P02_GroupId))
                                       .ToListAsync();

            // Build claims: name, id, groups, and permission claims
            var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
            new Claim(ClaimTypes.Name, user.FullName ?? user.UserLogin ?? string.Empty)
        };

            foreach (var gid in groupIds)
                claims.Add(new Claim("group", gid.ToString()));

            // permission claim format: PageId|ComponentId|isEnable|isVisible
            foreach (var pm in mappings)
            {
                if (pm.P05_PageComponentMapping == null)
                    continue;

                var pageId = pm.P05_PageComponentMapping.P01_PageId;
                var componentId = pm.P05_PageComponentMapping.P03_ComponentId;
                var permValue = $"{pageId}|{componentId}|{(pm.IsEnable ? 1 : 0)}|{(pm.IsVisible ? 1 : 0)}";
                claims.Add(new Claim("perm", permValue));
            }

            //var token = _jwtTokenService.GenerateToken(
            //    userId: user.UserID.ToString(),
            //    username: user.FullName ?? user.UserLogin ?? string.Empty
            //);

            // Trả về token và quyền để client có thể hiển thị UI tương ứng
            var permissions = mappings.Select(pm => new
            {
                PageId = pm.P05_PageComponentMapping?.P01_PageId,
                ComponentId = pm.P05_PageComponentMapping?.P03_ComponentId,
                pm.IsEnable,
                pm.IsVisible,
                GroupId = pm.P02_GroupId
            });

            return Ok(new { user = new { user.UserID, user.FullName, user.UserLogin }, permissions });
            //return Ok(new { accessToken = token, user = new { user.UserID, user.FullName, user.UserLogin }, permissions });
        }

        public record LoginRequest(string Username, string Password);
    }
}
