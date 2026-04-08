using gtas_vpp_be.Model.Auth;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Services;
using gtas_vpp_be.Services.Helpers.DTOs.Res;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static gtas_vpp_be.Service.Helpers.Config;


namespace ggtas_vpp_be.Controllers
{
    [ApiController]
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

            return Ok(data);
        }

        [HttpGet("groups/{id:guid}")]
        public async Task<IActionResult> GetGroupById(Guid id, [FromQuery] bool getFullName = true)
        {
            var data = await _bussinessService.BaseService<P02_Group>(
                EF_BASEMETHOD.EF_GetTByIdAsync, getFullName, Param: id);

            return Ok(data?.FirstOrDefault());
        }

        [HttpPut("groups/{id:guid}")]
        public async Task<IActionResult> UpdateGroup(Guid id, [FromBody] P02_Group model)
        {
            model.Id = id;
            var rs = await _bussinessService.BaseService<P02_Group>(
                EF_BASEMETHOD.EF_Update, objs: new List<P02_Group> { model });

            return Ok(rs?.FirstOrDefault() ?? model);
        }

        [HttpPut("component-mapping")]
        public async Task<IActionResult> UpdateComponentMapping([FromBody] P06_GroupPageComponentMapping model)
        {
            var rs = await _bussinessService.BaseService<P06_GroupPageComponentMapping>(
                EF_BASEMETHOD.EF_Update, objs: new List<P06_GroupPageComponentMapping> { model });

            return Ok(rs?.FirstOrDefault() ?? model);
        }

        [HttpDelete("groups/{id:guid}")]
        public async Task<IActionResult> DeleteGroup(Guid id)
        {
            var rs = await _bussinessService.BaseService<P02_Group>(
                EF_BASEMETHOD.EF_DeleteAsync, Param: id);

            return Ok(new { success = rs is not null });
        }
    }
}