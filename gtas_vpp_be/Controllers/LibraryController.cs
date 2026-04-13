using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Service.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using static gtas_vpp_be.Service.Helpers.Config;

namespace gtas_vpp_be.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LibraryController : ControllerBase
    {
        private readonly IBussinessService _bussinessService;

        public LibraryController(IBussinessService bussinessService)
        {
            _bussinessService = bussinessService;
        }

        [HttpGet("{tableCode}")]
        public async Task<IActionResult> GenericGet(string tableCode)
        {
            switch (tableCode.ToLower())
            {
                case "l01":
                    var dataL01 = await _bussinessService.BaseService<L01_Class>(EF_BASEMETHOD.EF_GetTAsync, true);
                    return Ok(dataL01 ?? new List<L01_Class>());
                case "l02":
                    var dataL02 = await _bussinessService.BaseService<L02_ClassDetail>(EF_BASEMETHOD.EF_GetTAsync, true);
                    return Ok(dataL02 ?? new List<L02_ClassDetail>());
                case "l03":
                    var dataL03 = await _bussinessService.BaseService<L03_VPPCategory>(EF_BASEMETHOD.EF_GetTAsync, true);
                    return Ok(dataL03 ?? new List<L03_VPPCategory>());
                case "l04":
                    var dataL04 = await _bussinessService.BaseService<L04_VPP>(EF_BASEMETHOD.EF_GetTAsync, true);
                    return Ok(dataL04 ?? new List<L04_VPP>());
                case "l05":
                    var dataL05 = await _bussinessService.BaseService<L05_VPPSupplier>(EF_BASEMETHOD.EF_GetTAsync, true);
                    return Ok(dataL05 ?? new List<L05_VPPSupplier>());
                case "l06":
                    var dataL06 = await _bussinessService.BaseService<L06_VPPSupplierMapping>(EF_BASEMETHOD.EF_GetTAsync, true);
                    return Ok(dataL06 ?? new List<L06_VPPSupplierMapping>());
                default:
                    return BadRequest(new { Message = $"Table Code '{tableCode}' is not supported." });
            }
        }

        [HttpGet("{tableCode}/{id:guid}")]
        public async Task<IActionResult> GenericGetById(string tableCode, Guid id)
        {
            switch (tableCode.ToLower())
            {
                case "l01":
                    var dataL01 = await _bussinessService.BaseService<L01_Class>(EF_BASEMETHOD.EF_GetTByIdAsync, true, Param: id);
                    return Ok(dataL01?.FirstOrDefault());
                case "l02":
                    var dataL02 = await _bussinessService.BaseService<L02_ClassDetail>(EF_BASEMETHOD.EF_GetTByIdAsync, true, Param: id);
                    return Ok(dataL02?.FirstOrDefault());
                case "l03":
                    var dataL03 = await _bussinessService.BaseService<L03_VPPCategory>(EF_BASEMETHOD.EF_GetTByIdAsync, true, Param: id);
                    return Ok(dataL03?.FirstOrDefault());
                case "l04":
                    var dataL04 = await _bussinessService.BaseService<L04_VPP>(EF_BASEMETHOD.EF_GetTByIdAsync, true, Param: id);
                    return Ok(dataL04?.FirstOrDefault());
                case "l05":
                    var dataL05 = await _bussinessService.BaseService<L05_VPPSupplier>(EF_BASEMETHOD.EF_GetTByIdAsync, true, Param: id);
                    return Ok(dataL05?.FirstOrDefault());
                case "l06":
                    var dataL06 = await _bussinessService.BaseService<L06_VPPSupplierMapping>(EF_BASEMETHOD.EF_GetTByIdAsync, true, Param: id);
                    return Ok(dataL06?.FirstOrDefault());
                default:
                    return BadRequest(new { Message = $"GetById for Table Code '{tableCode}' is not supported." });
            }
        }

        [HttpPost("{tableCode}")]
        public async Task<IActionResult> GenericCreate(string tableCode, [FromBody] JsonElement payload)
        {
            var json = payload.GetRawText();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            switch (tableCode.ToLower())
            {
                case "l01":
                    var l01 = JsonSerializer.Deserialize<L01_Class>(json, options);
                    if (l01 == null) return BadRequest();
                    l01.Id = Guid.Empty;
                    l01.CreateDate = DateTime.Now;
                    l01.UpdateDate = DateTime.Now;
                    var rsL01 = await _bussinessService.BaseService<L01_Class>(EF_BASEMETHOD.EF_Create, objs: new List<L01_Class> { l01 });
                    return Ok(rsL01?.FirstOrDefault());

                case "l02":
                    var l02 = JsonSerializer.Deserialize<L02_ClassDetail>(json, options);
                    if (l02 == null) return BadRequest();
                    l02.Id = Guid.Empty;
                    l02.CreateDate = DateTime.Now;
                    l02.UpdateDate = DateTime.Now;
                    var rsL02 = await _bussinessService.BaseService<L02_ClassDetail>(EF_BASEMETHOD.EF_Create, objs: new List<L02_ClassDetail> { l02 });
                    return Ok(rsL02?.FirstOrDefault());

                case "l03":
                    var l03 = JsonSerializer.Deserialize<L03_VPPCategory>(json, options);
                    if (l03 == null) return BadRequest();
                    l03.Id = Guid.Empty;
                    l03.CreateDate = DateTime.Now;
                    l03.UpdateDate = DateTime.Now;
                    var rsL03 = await _bussinessService.BaseService<L03_VPPCategory>(EF_BASEMETHOD.EF_Create, objs: new List<L03_VPPCategory> { l03 });
                    return Ok(rsL03?.FirstOrDefault());

                case "l04":
                    var l04 = JsonSerializer.Deserialize<L04_VPP>(json, options);
                    if (l04 == null) return BadRequest();
                    l04.Id = Guid.Empty;
                    l04.CreateDate = DateTime.Now;
                    l04.UpdateDate = DateTime.Now;
                    var rsL04 = await _bussinessService.BaseService<L04_VPP>(EF_BASEMETHOD.EF_Create, objs: new List<L04_VPP> { l04 });
                    return Ok(rsL04?.FirstOrDefault());

                case "l05":
                    var l05 = JsonSerializer.Deserialize<L05_VPPSupplier>(json, options);
                    if (l05 == null) return BadRequest();
                    l05.Id = Guid.Empty;
                    l05.CreateDate = DateTime.Now;
                    l05.UpdateDate = DateTime.Now;
                    var rsL05 = await _bussinessService.BaseService<L05_VPPSupplier>(EF_BASEMETHOD.EF_Create, objs: new List<L05_VPPSupplier> { l05 });
                    return Ok(rsL05?.FirstOrDefault());

                case "l06":
                    var l06 = JsonSerializer.Deserialize<L06_VPPSupplierMapping>(json, options);
                    if (l06 == null) return BadRequest();
                    l06.Id = Guid.Empty;
                    l06.CreateDate = DateTime.Now;
                    l06.UpdateDate = DateTime.Now;
                    var rsL06 = await _bussinessService.BaseService<L06_VPPSupplierMapping>(EF_BASEMETHOD.EF_Create, objs: new List<L06_VPPSupplierMapping> { l06 });
                    return Ok(rsL06?.FirstOrDefault());

                default:
                    return BadRequest(new { Message = $"Create for Table Code '{tableCode}' is not supported." });
            }
        }

        [HttpPut("{tableCode}")]
        public async Task<IActionResult> GenericUpdate(string tableCode, [FromBody] JsonElement payload)
        {
            var json = payload.GetRawText();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            switch (tableCode.ToLower())
            {
                case "l01":
                    var l01 = JsonSerializer.Deserialize<L01_Class>(json, options);
                    if (l01 == null) return BadRequest();
                    l01.UpdateDate = DateTime.Now;
                    var rsL01 = await _bussinessService.BaseService<L01_Class>(EF_BASEMETHOD.EF_Update, objs: new List<L01_Class> { l01 });
                    return Ok(rsL01?.FirstOrDefault());

                case "l02":
                    var l02 = JsonSerializer.Deserialize<L02_ClassDetail>(json, options);
                    if (l02 == null) return BadRequest();
                    l02.UpdateDate = DateTime.Now;
                    var rsL02 = await _bussinessService.BaseService<L02_ClassDetail>(EF_BASEMETHOD.EF_Update, objs: new List<L02_ClassDetail> { l02 });
                    return Ok(rsL02?.FirstOrDefault());

                case "l03":
                    var l03 = JsonSerializer.Deserialize<L03_VPPCategory>(json, options);
                    if (l03 == null) return BadRequest();
                    l03.UpdateDate = DateTime.Now;
                    var rsL03 = await _bussinessService.BaseService<L03_VPPCategory>(EF_BASEMETHOD.EF_Update, objs: new List<L03_VPPCategory> { l03 });
                    return Ok(rsL03?.FirstOrDefault());

                case "l04":
                    var l04 = JsonSerializer.Deserialize<L04_VPP>(json, options);
                    if (l04 == null) return BadRequest();
                    l04.UpdateDate = DateTime.Now;
                    var rsL04 = await _bussinessService.BaseService<L04_VPP>(EF_BASEMETHOD.EF_Update, objs: new List<L04_VPP> { l04 });
                    return Ok(rsL04?.FirstOrDefault());

                case "l05":
                    var l05 = JsonSerializer.Deserialize<L05_VPPSupplier>(json, options);
                    if (l05 == null) return BadRequest();
                    l05.UpdateDate = DateTime.Now;
                    var rsL05 = await _bussinessService.BaseService<L05_VPPSupplier>(EF_BASEMETHOD.EF_Update, objs: new List<L05_VPPSupplier> { l05 });
                    return Ok(rsL05?.FirstOrDefault());

                case "l06":
                    var l06 = JsonSerializer.Deserialize<L06_VPPSupplierMapping>(json, options);
                    if (l06 == null) return BadRequest();
                    l06.UpdateDate = DateTime.Now;
                    var rsL06 = await _bussinessService.BaseService<L06_VPPSupplierMapping>(EF_BASEMETHOD.EF_Update, objs: new List<L06_VPPSupplierMapping> { l06 });
                    return Ok(rsL06?.FirstOrDefault());

                default:
                    return BadRequest(new { Message = $"Update for Table Code '{tableCode}' is not supported." });
            }
        }

        [HttpDelete("{tableCode}/{id:guid}")]
        public async Task<IActionResult> GenericDelete(string tableCode, Guid id)
        {
            bool isSuccess = false;
            switch (tableCode.ToLower())
            {
                case "l01":
                    var rsL01 = await _bussinessService.BaseService<L01_Class>(EF_BASEMETHOD.EF_DeleteAsync, Param: id);
                    isSuccess = rsL01 != null;
                    break;
                case "l02":
                    var rsL02 = await _bussinessService.BaseService<L02_ClassDetail>(EF_BASEMETHOD.EF_DeleteAsync, Param: id);
                    isSuccess = rsL02 != null;
                    break;
                case "l03":
                    var rsL03 = await _bussinessService.BaseService<L03_VPPCategory>(EF_BASEMETHOD.EF_DeleteAsync, Param: id);
                    isSuccess = rsL03 != null;
                    break;
                case "l04":
                    var rsL04 = await _bussinessService.BaseService<L04_VPP>(EF_BASEMETHOD.EF_DeleteAsync, Param: id);
                    isSuccess = rsL04 != null;
                    break;
                case "l05":
                    var rsL05 = await _bussinessService.BaseService<L05_VPPSupplier>(EF_BASEMETHOD.EF_DeleteAsync, Param: id);
                    isSuccess = rsL05 != null;
                    break;
                case "l06":
                    var rsL06 = await _bussinessService.BaseService<L06_VPPSupplierMapping>(EF_BASEMETHOD.EF_DeleteAsync, Param: id);
                    isSuccess = rsL06 != null;
                    break;
                default:
                    return BadRequest(new { Message = $"Delete for Table Code '{tableCode}' is not supported." });
            }
            return Ok(new { success = isSuccess });
        }
    }
}
