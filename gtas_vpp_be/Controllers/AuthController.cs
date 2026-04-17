using gtas_vpp_be.Model;
//using gtas_vpp_be.Model.View;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Helpers.Context;
using gtas_vpp_shared.DTOs.Res;
using gtas_vpp_be.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using gtas_vpp_shared.DTOs.Res.Auth;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace gtas_vpp_be.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly VPPContext _authDb;
        private readonly IBussinessService _bussinessService;
        private readonly IConfiguration _configuration;

        public AuthController(VPPContext authDb, IBussinessService bussinessService, IConfiguration configuration)
        {
            _authDb = authDb;
            _bussinessService = bussinessService;
            _configuration = configuration;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new { message = "Username and password are required." });

            #region SP
            try
            {
                var result = await _bussinessService.SP(
                 "sp_Authen",
                 "sp_Authen_Login",
                 new
                 {
                     UserLogin = request.Username,
                     PasswordChar = PasswordHelpers.Encrypt(request.Password, true)
                 }
             );

                if (!result.IsSuccess || string.IsNullOrEmpty(result.ResData))
                    return Unauthorized(new { message = result.ErrorMess ?? "Login failed" });

                var loginData = JsonConvert.DeserializeObject<sp_Authentication_Login>(result.ResData);
                Console.WriteLine(
                    $"[LOGIN] user={request.Username} | FullName={loginData?.FullName} | MemberCompanyName={loginData?.MemberCompanyName} | DepartmentCode={loginData?.DepartmentCode}");

                if (loginData == null)
                    return Unauthorized(new { message = "Invalid username or password." });

                loginData.AccessToken = GenerateAccessToken(loginData);
                return Ok(loginData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred during login", details = ex.Message });
            }
            #endregion
        }

        private string GenerateAccessToken(sp_Authentication_Login loginData)
        {
            var jwtKey = _configuration["JwtSettings:Key"] ?? "GTAS_VPP_BE_DEV_ONLY_KEY_CHANGE_IN_PRODUCTION_2026";
            var jwtIssuer = _configuration["JwtSettings:Issuer"] ?? "gtas_vpp_be";
            var jwtAudience = _configuration["JwtSettings:Audience"] ?? "gtas_vpp_clients";

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, loginData.UserID.ToString()),
                new(ClaimTypes.Name, loginData.FullName ?? loginData.UserLogin ?? string.Empty),
                new("UserID", loginData.UserID.ToString()),
                new("UserLogin", loginData.UserLogin ?? string.Empty),
                new("GroupId", loginData.GroupId.ToString()),
                new("IsAdmin", loginData.IsAdmin.ToString())
            };

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(12),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public record LoginRequest(string Username, string Password);
    }
}
