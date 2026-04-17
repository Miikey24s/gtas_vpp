using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace gtas_vpp_be.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class LibraryController : BaseGenericController
    {
        public LibraryController(IBussinessService bussinessService) : base(bussinessService) { }

        [HttpGet("{tableCode}")]
        public async Task<IActionResult> GenericGet(string tableCode, [FromQuery] Guid? id, [FromQuery] string? searchText)
        {
            string cleanSearch = searchText?.Trim() ?? string.Empty;

            return tableCode.ToLower() switch
            {
                "l01" => await GetTableDataAsync<L01_Class>(id, cleanSearch,
                    matchId: x => x.Id == id,
                    matchSearch: x => x.ClassName.Contains(cleanSearch)
                                   || x.ClassCode.Contains(cleanSearch)
                                   || x.Description.Contains(cleanSearch)),
                "l02" => await GetTableDataAsync<L02_ClassDetail>(id, cleanSearch,
                    matchId: x => x.Id == id,
                    matchSearch: x => x.ClassDetailCode.Contains(cleanSearch)
                                   || x.ClassDetailValue!.Contains(cleanSearch)
                                   || x.Description.Contains(cleanSearch)),
                "l03" => await GetTableDataAsync<L03_VPPCategory>(id, cleanSearch, matchId: x => x.Id == id),
                "l04" => await GetTableDataAsync<L04_VPP>(id, cleanSearch, matchId: x => x.Id == id),
                "l05" => await GetTableDataAsync<L05_VPPSupplier>(id, cleanSearch, matchId: x => x.Id == id),
                "l06" => await GetTableDataAsync<L06_VPPSupplierMapping>(id, cleanSearch, matchId: x => x.Id == id),
                _ => BadRequest(new { Message = $"Table Code '{tableCode}' is not supported." })
            };
        }

        [HttpGet("{tableCode}/{id:guid}")]
        public async Task<IActionResult> GenericGetById(string tableCode, Guid id)
        {
            return tableCode.ToLower() switch
            {
                "l01" => await GetByIdAsync<L01_Class>(id),
                "l02" => await GetByIdAsync<L02_ClassDetail>(id),
                "l03" => await GetByIdAsync<L03_VPPCategory>(id),
                "l04" => await GetByIdAsync<L04_VPP>(id),
                "l05" => await GetByIdAsync<L05_VPPSupplier>(id),
                "l06" => await GetByIdAsync<L06_VPPSupplierMapping>(id),
                _ => BadRequest(new { Message = $"GetById for Table Code '{tableCode}' is not supported." })
            };
        }

        [HttpPost("{tableCode}")]
        public async Task<IActionResult> GenericCreate(string tableCode, [FromBody] JsonElement payload)
        {
            var json = payload.GetRawText();
            return tableCode.ToLower() switch
            {
                "l01" => await CreateAsync<L01_Class>(json),
                "l02" => await CreateAsync<L02_ClassDetail>(json),
                "l03" => await CreateAsync<L03_VPPCategory>(json),
                "l04" => await CreateAsync<L04_VPP>(json),
                "l05" => await CreateAsync<L05_VPPSupplier>(json),
                "l06" => await CreateAsync<L06_VPPSupplierMapping>(json),
                _ => BadRequest(new { Message = $"Create for Table Code '{tableCode}' is not supported." })
            };
        }

        [HttpPut("{tableCode}")]
        public async Task<IActionResult> GenericUpdate(string tableCode, [FromBody] JsonElement payload)
        {
            var json = payload.GetRawText();
            return tableCode.ToLower() switch
            {
                "l01" => await UpdateAsync<L01_Class>(json),
                "l02" => await UpdateAsync<L02_ClassDetail>(json),
                "l03" => await UpdateAsync<L03_VPPCategory>(json),
                "l04" => await UpdateAsync<L04_VPP>(json),
                "l05" => await UpdateAsync<L05_VPPSupplier>(json),
                "l06" => await UpdateAsync<L06_VPPSupplierMapping>(json),
                _ => BadRequest(new { Message = $"Update for Table Code '{tableCode}' is not supported." })
            };
        }

        [HttpPatch("{tableCode}/{id:guid}")]
        public async Task<IActionResult> GenericPatch(string tableCode, Guid id, [FromBody] JsonElement payload)
        {
            if (payload.ValueKind == JsonValueKind.Undefined || payload.ValueKind == JsonValueKind.Null)
            {
                return BadRequest(new { Message = "Update payload must not be empty." });
            }

            return tableCode.ToLower() switch
            {
                "l01" => await ApplyPatchAsync<L01_Class>(id, payload),
                "l02" => await ApplyPatchAsync<L02_ClassDetail>(id, payload),
                "l03" => await ApplyPatchAsync<L03_VPPCategory>(id, payload),
                "l04" => await ApplyPatchAsync<L04_VPP>(id, payload),
                "l05" => await ApplyPatchAsync<L05_VPPSupplier>(id, payload),
                "l06" => await ApplyPatchAsync<L06_VPPSupplierMapping>(id, payload),
                _ => BadRequest(new { Message = $"Patch for Table Code '{tableCode}' is not supported." })
            };
        }

        [HttpDelete("{tableCode}/{id:guid}")]
        public async Task<IActionResult> GenericDelete(string tableCode, Guid id)
        {
            return tableCode.ToLower() switch
            {
                "l01" => await DeleteAsync<L01_Class>(id),
                "l02" => await DeleteAsync<L02_ClassDetail>(id),
                "l03" => await DeleteAsync<L03_VPPCategory>(id),
                "l04" => await DeleteAsync<L04_VPP>(id),
                "l05" => await DeleteAsync<L05_VPPSupplier>(id),
                "l06" => await DeleteAsync<L06_VPPSupplierMapping>(id),
                _ => BadRequest(new { Message = $"Delete for Table Code '{tableCode}' is not supported." })
            };
        }
    }
}
