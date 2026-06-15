using gtas_vpp_be.Model;
using gtas_vpp_be.Model.Auth;
using gtas_vpp_be.Model.VPP;
using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Helpers.Context;
using gtas_vpp_shared.DTOs;
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
using Serilog;
using static gtas_vpp_be.Service.Helpers.Config;

namespace gtas_vpp_be.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly VPPContext _authDb;
        private readonly IStoredProcedureExecutor _storedProcedureExecutor;
        private readonly IGenericRepository<P04_UserGroup> _userGroupRepository;
        private readonly IGenericRepository<LEX02_CompanyDepartmentLocation> _departmentRepository;
        private readonly IUserNameResolver _userNameResolver;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;
        private readonly IPasswordEncoder _passwordEncoder;

        public AuthController(
            VPPContext authDb,
            IStoredProcedureExecutor storedProcedureExecutor,
            IGenericRepository<P04_UserGroup> userGroupRepository,
            IGenericRepository<LEX02_CompanyDepartmentLocation> departmentRepository,
            IUserNameResolver userNameResolver,
            IUnitOfWork unitOfWork,
            IConfiguration configuration,
            IPasswordEncoder passwordEncoder)
        {
            _authDb = authDb;
            _storedProcedureExecutor = storedProcedureExecutor;
            _userGroupRepository = userGroupRepository;
            _departmentRepository = departmentRepository;
            _userNameResolver = userNameResolver;
            _unitOfWork = unitOfWork;
            _configuration = configuration;
            _passwordEncoder = passwordEncoder;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new { message = "Username and password are required." });

            var server = ResolveAvailableServer(request.Server);
            if (server == null)
                return BadRequest(new { message = "The requested server is not available in this environment." });

            try
            {
                var result = await LoginWithTripleDesAsync(request.Username, request.Password);

                if (!result.IsSuccess || string.IsNullOrEmpty(result.ResData))
                    return Unauthorized(new { message = result.ErrorMess ?? "Login failed" });

                var loginData = JsonConvert.DeserializeObject<sp_Authentication_Login>(result.ResData);

                if (loginData == null)
                    return Unauthorized(new { message = "Invalid username or password." });

                await LoadDepartmentLocationAsync(loginData);

                loginData.AccessToken = GenerateAccessToken(loginData, server);

                Serilog.Log.Information("Login success: User={Username}, IP={IP}", request.Username, HttpContext.Connection.RemoteIpAddress);

                return Ok(loginData);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "Login failed: User={Username}, IP={IP}", request.Username, HttpContext.Connection.RemoteIpAddress);
                return StatusCode(500, new { message = "An error occurred during login" });
            }
        }

        private async Task<sp_ResDTO> LoginWithTripleDesAsync(string username, string password)
        {
            var encrypted = _passwordEncoder.Encrypt(password);
            var result = await _storedProcedureExecutor.ExecuteSPAsync(
                "sp_Authen", "sp_Authen_Login",
                new { UserLogin = username, PasswordChar = encrypted }
            );

            return result;
        }

        private async Task LoadDepartmentLocationAsync(sp_Authentication_Login loginData)
        {
            try
            {
                bool isCodeMissing = string.IsNullOrWhiteSpace(loginData.DepartmentCode);
                bool isNameMissing = string.IsNullOrWhiteSpace(loginData.DepartmentName);

                if (!isCodeMissing && !isNameMissing)
                    return; // Already have department info

                var userGroups = await _userGroupRepository.ReadAsync(x => x.UserId == loginData.UserID);
                userGroups = await _userNameResolver.WithUserNamesAsync(userGroups, _unitOfWork.VPPContext);

                var userGroup = userGroups?.FirstOrDefault();

                if (userGroup == null || userGroup.LEX02_CompanyDepartmentLocationId == Guid.Empty)
                    return;

                var departments = await _departmentRepository.ReadAsync(x => x.Id == userGroup.LEX02_CompanyDepartmentLocationId && !x.IsDeleted);

                var department = departments?.FirstOrDefault();

                if (department != null)
                {
                    if (isCodeMissing && !string.IsNullOrWhiteSpace(department.LEX02Code))
                    {
                        loginData.DepartmentCode = department.LEX02Code;
                    }

                    if (isNameMissing && !string.IsNullOrWhiteSpace(department.LEX02Name))
                    {
                        loginData.DepartmentName = department.LEX02Name;
                    }
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "Failed to load department location for UserId={UserId}", loginData.UserID);
            }
        }

        private string GenerateAccessToken(sp_Authentication_Login loginData, string? server)
        {
            var jwtKey = _configuration["JwtSettings:Key"];
            if (string.IsNullOrWhiteSpace(jwtKey))
            {
                throw new InvalidOperationException("JWT Key must be configured via environment variable or user secrets");
            }

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

            // Add DepartmentCode if available
            if (!string.IsNullOrWhiteSpace(loginData.DepartmentCode))
            {
                claims.Add(new Claim("DepartmentCode", loginData.DepartmentCode));
            }

            if (!string.IsNullOrWhiteSpace(server))
            {
                claims.Add(new Claim("Server", server));
            }

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(24),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string? ResolveAvailableServer(string? requestedServer)
        {
            var requestedEnvironment = requestedServer?.Trim() switch
            {
                var value when value?.Equals("Test", StringComparison.OrdinalIgnoreCase) == true => "TestEnv",
                var value when value?.Equals("Live", StringComparison.OrdinalIgnoreCase) == true => "LiveEnv",
                null or "" => _configuration["DatabaseSettings:DefaultEnvironment"] ?? "TestEnv",
                _ => null
            };

            if (requestedEnvironment == null
                || string.IsNullOrWhiteSpace(_configuration.GetConnectionString(requestedEnvironment)))
            {
                return null;
            }

            return requestedEnvironment.Equals("LiveEnv", StringComparison.OrdinalIgnoreCase)
                ? "Live"
                : "Test";
        }

        public record LoginRequest(string Username, string Password, string? Server = null);
    }
}
