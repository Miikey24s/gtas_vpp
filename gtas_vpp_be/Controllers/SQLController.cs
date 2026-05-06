using gtas_vpp_be.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;
using System.Web;

namespace gtas_vpp_be.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]/[action]")]
    public class SQLController : ControllerBase
    {
        private static readonly HashSet<string> AllowedStoredProcedures = new(StringComparer.OrdinalIgnoreCase)
        {
            "sp_Authen"
        };

        private readonly IStoredProcedureExecutor _storedProcedureExecutor;

        public SQLController(IStoredProcedureExecutor storedProcedureExecutor)
        {
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

            if (!AllowedStoredProcedures.Contains(spName))
            {
                return BadRequest("Stored procedure is not allowed.");
            }

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
                Serilog.Log.Warning(ex, "SQL execution failed: SP={SpName}, Type={SpType}", spName, sptype);
                base.HttpContext.Response.Headers.Append("script", script);
                return BadRequest("An error occurred while executing the query.");
            }

            base.HttpContext.Response.Headers.Append("script", script);
            return Ok(empty);
        }

        [HttpGet("test")]
        public IActionResult Test()
        {
            var username = User.Identity?.Name;
            return Ok($"Hello {username}");
        }
    }

}
