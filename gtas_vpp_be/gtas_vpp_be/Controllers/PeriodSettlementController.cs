using gtas_vpp_be.Service.Services;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.Req.VPP;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace gtas_vpp_be.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class PeriodSettlementController : ControllerBase
    {
        private readonly IPeriodSettlementService _periodSettlementService;

        public PeriodSettlementController(IPeriodSettlementService periodSettlementService)
        {
            _periodSettlementService = periodSettlementService;
        }

        private int? CurrentUserId => int.TryParse(User.FindFirstValue("UserID"), out var id) ? id : null;

        [HttpPost("settle")]
        [Authorize(Policy = Permissions.PeriodSettle)]
        public async Task<IActionResult> Settle([FromBody] VPP_SettlePeriodReqDTO req)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });

            return Ok(await _periodSettlementService.SettleAsync(req, CurrentUserId.Value));
        }

        [HttpGet("{y:int}/{m:int}")]
        [Authorize(Policy = Permissions.PeriodSettle)]
        public async Task<IActionResult> GetStatus(int y, int m)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });

            return Ok(await _periodSettlementService.GetStatusAsync(y, m));
        }

        [HttpGet]
        [Authorize(Policy = Permissions.PeriodSettle)]
        public async Task<IActionResult> ListSettled()
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });

            return Ok(await _periodSettlementService.ListSettledAsync());
        }
    }
}
