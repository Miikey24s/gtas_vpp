using gtas_vpp_be.Model.Auth;
using gtas_vpp_be.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;
using System.Text.Json;
using System.Web;
using static gtas_vpp_be.Service.Helpers.Config;

namespace gtas_vpp_be.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]/[action]")]
    public class SQLController : ControllerBase
    {
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IStoredProcedureExecutor _storedProcedureExecutor;
        public SQLController(IHttpContextAccessor contextAccessor, IStoredProcedureExecutor storedProcedureExecutor)
        {
            _contextAccessor = contextAccessor;
            _storedProcedureExecutor = storedProcedureExecutor;
        }
        private void SetHeader(string? script)
        {
            var check = Encoding.UTF8.GetBytes(script ?? string.Empty).Any(b => b > 127);
            var str = check ? HttpUtility.UrlEncode(script) : script;
            if (str != null && Encoding.UTF8.GetByteCount(str) < 60000)
                HttpContext.Response.Headers.Append("script", str);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="spName">Tên sp, vd như sp_Authen, sp_Library...</param>
        /// <param name="isMultiple">Kết quả trả về là nhiều kết quả hay 1 kết quả, [] hay {}</param>
        /// <param name="sptype">Sp chi tiết, vd như sp_Authen_Login</param>
        /// <param name="param">Tham số truyền vào theo từng sptype</param>
        /// <returns></returns>
        [HttpPost("{spName}")]
        public virtual async Task<IActionResult> StoreProcedure(string spName, string sptype, int? timeout = 300, [FromBody] object? param = null)
        {
            string script = string.Empty;
            string empty = string.Empty;
            try
            {
                object? desParam = JsonConvert.DeserializeObject<object>(param?.ToString() ?? "");
                var rs = await _storedProcedureExecutor.ExecuteSPAsync(spName, sptype, param == null ? new { } : desParam!, timeout);
                empty = JsonConvert.SerializeObject(rs,
                                new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
                //empty = JsonConvert.SerializeObject(_jsonRepo.ExecuteStoreProcedure(spName, param, out string _, out script));
            }
            catch (Exception ex)
            {
                //_storedProcedureExecutor.WriteLog(ex, sptype, new Dictionary<string, object>
                //{
                //    { "spName", spName },
                //    { "sptype", sptype },
                //    { "param", param ?? string.Empty }
                //});
                base.HttpContext.Response.Headers.Append("script", script);
                return BadRequest(ex.Message);
            }

            base.HttpContext.Response.Headers.Append("script", script);
            return Ok(empty);
        }

        [HttpPost]
        public virtual async Task<IActionResult> Query([FromBody] SqlQueryRequest? request, int? timeout = 300)
        {
            string script = string.Empty;
            string empty = string.Empty;
            try
            {
                if (request is null || string.IsNullOrWhiteSpace(request.Query))
                {
                    return BadRequest("Query is required.");
                }

                var parameters = NormalizeSqlParameters(request.Parameters);
                var rs = await _storedProcedureExecutor.ExecuteQueryAsync(request.Query, timeout, parameters);
                empty = JsonConvert.SerializeObject(rs,
                                new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
            }
            catch (Exception ex)
            {
                base.HttpContext.Response.Headers.Append("script", script);
                return BadRequest(ex.Message);
            }
            base.HttpContext.Response.Headers.Append("script", script);
            return Ok(empty);
        }

        private static object?[] NormalizeSqlParameters(IEnumerable<JsonElement>? parameters)
        {
            return parameters?.Select(NormalizeSqlParameter).ToArray() ?? Array.Empty<object?>();
        }

        private static object? NormalizeSqlParameter(JsonElement parameter)
        {
            return parameter.ValueKind switch
            {
                JsonValueKind.Null or JsonValueKind.Undefined => null,
                JsonValueKind.String => parameter.GetString(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Number when parameter.TryGetInt32(out var intValue) => intValue,
                JsonValueKind.Number when parameter.TryGetInt64(out var longValue) => longValue,
                JsonValueKind.Number when parameter.TryGetDecimal(out var decimalValue) => decimalValue,
                _ => parameter.GetRawText()
            };
        }

        [HttpGet("test")]
        public IActionResult Test()
        {
            var username = User.Identity?.Name;
            return Ok($"Hello {username}");
        }
    }

    public sealed class SqlQueryRequest
    {
        public string Query { get; init; } = string.Empty;
        public IReadOnlyList<JsonElement> Parameters { get; init; } = Array.Empty<JsonElement>();
    }
}
