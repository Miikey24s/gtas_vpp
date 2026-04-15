using gtas_vpp_be.Model.VPP;
using gtas_vpp_be.Service.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace gtas_vpp_be.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VPPRequestController : BaseGenericController
    {
        public VPPRequestController(IBussinessService bussinessService) : base(bussinessService) { }

        [HttpGet("{tableCode}")]
        public async Task<IActionResult> GenericGet(string tableCode, [FromQuery] Guid? id, [FromQuery] string? searchText)
        {
            string cleanSearch = searchText?.Trim() ?? string.Empty;

            return tableCode.ToLower() switch
            {
                "vpp01" => await GetTableDataAsync<VPP01_RequestHeader>(id, cleanSearch,
                    matchId: x => x.Id == id,
                    matchSearch: x => x.VPPCode!.Contains(cleanSearch)
                                   || x.Description!.Contains(cleanSearch)),
                "vpp02" => await GetTableDataAsync<VPP02_RequestDetail>(id, cleanSearch,
                    matchId: x => x.Id == id,
                    matchSearch: x => x.Description!.Contains(cleanSearch)),
                _ => BadRequest(new { Message = $"Table Code '{tableCode}' is not supported." })
            };
        }

        [HttpGet("{tableCode}/{id:guid}")]
        public async Task<IActionResult> GenericGetById(string tableCode, Guid id)
        {
            return tableCode.ToLower() switch
            {
                "vpp01" => await GetByIdAsync<VPP01_RequestHeader>(id),
                "vpp02" => await GetByIdAsync<VPP02_RequestDetail>(id),
                _ => BadRequest(new { Message = $"GetById for Table Code '{tableCode}' is not supported." })
            };
        }

        [HttpPost("{tableCode}")]
        public async Task<IActionResult> GenericCreate(string tableCode, [FromBody] JsonElement payload)
        {
            var json = payload.GetRawText();
            return tableCode.ToLower() switch
            {
                "vpp01" => await CreateAsync<VPP01_RequestHeader>(json),
                "vpp02" => await CreateAsync<VPP02_RequestDetail>(json),
                _ => BadRequest(new { Message = $"Create for Table Code '{tableCode}' is not supported." })
            };
        }

        [HttpPut("{tableCode}")]
        public async Task<IActionResult> GenericUpdate(string tableCode, [FromBody] JsonElement payload)
        {
            var json = payload.GetRawText();
            return tableCode.ToLower() switch
            {
                "vpp01" => await UpdateAsync<VPP01_RequestHeader>(json),
                "vpp02" => await UpdateAsync<VPP02_RequestDetail>(json),
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
                "vpp01" => await ApplyPatchAsync<VPP01_RequestHeader>(id, payload),
                "vpp02" => await ApplyPatchAsync<VPP02_RequestDetail>(id, payload),
                _ => BadRequest(new { Message = $"Patch for Table Code '{tableCode}' is not supported." })
            };
        }

        [HttpDelete("{tableCode}/{id:guid}")]
        public async Task<IActionResult> GenericDelete(string tableCode, Guid id)
        {
            return tableCode.ToLower() switch
            {
                "vpp01" => await DeleteAsync<VPP01_RequestHeader>(id),
                "vpp02" => await DeleteAsync<VPP02_RequestDetail>(id),
                _ => BadRequest(new { Message = $"Delete for Table Code '{tableCode}' is not supported." })
            };
        }
    }
}
