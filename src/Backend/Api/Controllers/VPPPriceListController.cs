using gtas_vpp_be.Service.Services;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.Req.Library;
using gtas_vpp_shared.DTOs.Res.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using System.Text.Json;

namespace gtas_vpp_be.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class VPPPriceListController : ControllerBase
    {
        private readonly IPriceListService _priceListService;
        private readonly IPriceBookWorkflowService? _workflowService;
        private readonly IPriceListImportService? _importService;
        private readonly IPriceListExportService? _exportService;

        public VPPPriceListController(IPriceListService priceListService)
            : this(priceListService, null, null, null)
        {
        }

        [ActivatorUtilitiesConstructor]
        public VPPPriceListController(
            IPriceListService priceListService,
            IPriceBookWorkflowService? workflowService,
            IPriceListImportService? importService,
            IPriceListExportService? exportService = null)
        {
            _priceListService = priceListService;
            _workflowService = workflowService;
            _importService = importService;
            _exportService = exportService;
        }

        private int? CurrentUserId => int.TryParse(User.FindFirstValue("UserID"), out var id) ? id : null;

        [HttpGet]
        [Authorize(Policy = Permissions.LibraryView)]
        [ProducesResponseType<List<PriceListResDTO>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> List(
            [FromQuery] bool? showDeleted = false,
            [FromQuery] string? search = null,
            [FromQuery] string? filter = null,
            [FromQuery] int? skip = null,
            [FromQuery] int? top = null,
            [FromQuery] string? orderby = null,
            [FromQuery] string? distinct = null,
            [FromQuery] string? distinctFilter = null)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });

            var isGridRequest = !string.IsNullOrWhiteSpace(search)
                                || !string.IsNullOrWhiteSpace(filter)
                                || skip.HasValue
                                || top.HasValue
                                || !string.IsNullOrWhiteSpace(orderby)
                                || !string.IsNullOrWhiteSpace(distinct)
                                || !string.IsNullOrWhiteSpace(distinctFilter);

            if (isGridRequest)
            {
                var result = await _priceListService.QueryAsync(showDeleted == true, search, filter, skip, top, orderby, distinct, distinctFilter);
                Response.Headers.Append("X-Total-Count", result.TotalCount.ToString());
                return Ok(result.Data);
            }

            return Ok(await _priceListService.ListAsync(showDeleted == true));
        }

        [HttpGet("{id:guid}")]
        [Authorize(Policy = Permissions.LibraryView)]
        [ProducesResponseType<PriceListResDTO>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });

            var result = await _priceListService.GetByIdAsync(id);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpPost]
        [Authorize(Policy = Permissions.LibraryManage)]
        [ProducesResponseType<PriceListResDTO>(StatusCodes.Status200OK)]
        public async Task<IActionResult> Create([FromBody] PriceListCreateReqDTO req)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });

            return Ok(await _priceListService.CreateAsync(req, CurrentUserId.Value));
        }

        [HttpPut("{id:guid}")]
        [Authorize(Policy = Permissions.LibraryManage)]
        [ProducesResponseType<PriceListResDTO>(StatusCodes.Status200OK)]
        public async Task<IActionResult> Update(Guid id, [FromBody] PriceListUpdateReqDTO req)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });

            req.Id = id;
            return Ok(await _priceListService.UpdateAsync(req, CurrentUserId.Value));
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Policy = Permissions.LibraryManage)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });

            await _priceListService.DeleteAsync(id, CurrentUserId.Value);
            return NoContent();
        }

        [HttpPatch("{id:guid}/deleted")]
        [Authorize(Policy = Permissions.LibraryManage)]
        [ProducesResponseType<PriceListResDTO>(StatusCodes.Status200OK)]
        public async Task<IActionResult> SetDeleted(Guid id, [FromBody] JsonElement payload)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });

            if (!payload.TryGetProperty("isDeleted", out var isDeletedJson)
                && !payload.TryGetProperty("IsDeleted", out isDeletedJson))
            {
                return BadRequest(new { Message = "IsDeleted is required." });
            }

            var result = await _priceListService.SetDeletedAsync(id, isDeletedJson.GetBoolean(), CurrentUserId.Value);
            return Ok(result);
        }

        [HttpDelete("{id:guid}/hard")]
        [Authorize(Policy = Permissions.LibraryManage)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> HardDelete(Guid id)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });

            await _priceListService.HardDeleteAsync(id);
            return NoContent();
        }

        [HttpPost("{id:guid}/set-default")]
        [Authorize(Policy = Permissions.LibraryManage)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> SetDefault(Guid id)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });

            await _priceListService.SetDefaultAsync(id, CurrentUserId.Value);
            return NoContent();
        }

        [HttpPost("clone")]
        [Authorize(Policy = Permissions.LibraryManage)]
        [ProducesResponseType<PriceListResDTO>(StatusCodes.Status200OK)]
        public async Task<IActionResult> Clone([FromBody] PriceListCloneReqDTO req)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });

            return Ok(await _priceListService.CloneAsync(req, CurrentUserId.Value));
        }

        [HttpPost("{id:guid}/publish")]
        [Authorize(Policy = Permissions.LibraryManage)]
        [ProducesResponseType<PriceListResDTO>(StatusCodes.Status200OK)]
        public async Task<IActionResult> Publish(
            Guid id,
            [FromBody] PriceBookStatusReqDTO req,
            CancellationToken cancellationToken)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });
            if (_workflowService is null) return StatusCode(StatusCodes.Status503ServiceUnavailable);
            return Ok(await _workflowService.PublishAsync(id, req, CurrentUserId.Value, cancellationToken));
        }

        [HttpPost("{id:guid}/expire")]
        [Authorize(Policy = Permissions.LibraryManage)]
        [ProducesResponseType<PriceListResDTO>(StatusCodes.Status200OK)]
        public async Task<IActionResult> Expire(
            Guid id,
            [FromBody] PriceBookStatusReqDTO req,
            CancellationToken cancellationToken)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });
            if (_workflowService is null) return StatusCode(StatusCodes.Status503ServiceUnavailable);
            return Ok(await _workflowService.ExpireAsync(id, req, CurrentUserId.Value, cancellationToken));
        }

        [HttpPost("compare")]
        [Authorize(Policy = Permissions.LibraryView)]
        [ProducesResponseType<PriceBookComparisonResDTO>(StatusCodes.Status200OK)]
        public async Task<IActionResult> Compare(
            [FromBody] PriceBookComparisonReqDTO req,
            CancellationToken cancellationToken)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });
            if (_workflowService is null) return StatusCode(StatusCodes.Status503ServiceUnavailable);
            return Ok(await _workflowService.CompareAsync(req, cancellationToken));
        }

        [HttpGet("{id:guid}/imports/template.xlsx")]
        [Authorize(Policy = Permissions.LibraryManage)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> DownloadImportTemplate(Guid id, CancellationToken cancellationToken)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });
            if (_importService is null) return StatusCode(StatusCodes.Status503ServiceUnavailable);

            var template = await _importService.BuildTemplateAsync(id, cancellationToken);
            return File(template.Content, template.ContentType, template.FileName);
        }

        [HttpGet("imports/template.xlsx")]
        [Authorize(Policy = Permissions.LibraryManage)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> DownloadGenericImportTemplate(CancellationToken cancellationToken)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });
            if (_importService is null) return StatusCode(StatusCodes.Status503ServiceUnavailable);

            var template = await _importService.BuildTemplateAsync(null, cancellationToken);
            return File(template.Content, template.ContentType, template.FileName);
        }

        [HttpGet("{id:guid}/export.xlsx")]
        [Authorize(Policy = Permissions.LibraryView)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> ExportExcel(Guid id, CancellationToken cancellationToken)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });
            if (_exportService is null) return StatusCode(StatusCodes.Status503ServiceUnavailable);

            var export = await _exportService.ExportExcelAsync(id, cancellationToken);
            return File(export.Content, export.ContentType, export.FileName);
        }

        [HttpPost("{id:guid}/imports/preview")]
        [Authorize(Policy = Permissions.LibraryManage)]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(PriceListImportFileParser.MaximumFileSizeBytes)]
        [ProducesResponseType<PriceListImportPreviewResDTO>(StatusCodes.Status200OK)]
        public async Task<IActionResult> PreviewImport(
            Guid id,
            [FromForm] IFormFile? file,
            [FromForm] string? columnMappingsJson,
            CancellationToken cancellationToken)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });
            if (_importService is null) return StatusCode(StatusCodes.Status503ServiceUnavailable);
            if (file is null || file.Length == 0) return BadRequest(new { Message = "File is required." });

            Dictionary<int, string>? columnMappings = null;
            if (!string.IsNullOrWhiteSpace(columnMappingsJson))
            {
                try
                {
                    columnMappings = JsonSerializer.Deserialize<Dictionary<int, string>>(columnMappingsJson);
                }
                catch (JsonException)
                {
                    return BadRequest(new { Message = "Column mappings are invalid." });
                }
            }

            await using var stream = file.OpenReadStream();
            return Ok(await _importService.PreviewAsync(
                id,
                file.FileName,
                stream,
                columnMappings,
                CurrentUserId.Value,
                cancellationToken));
        }

        [HttpPost("{id:guid}/imports/analyze")]
        [Authorize(Policy = Permissions.LibraryManage)]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(PriceListImportFileParser.MaximumFileSizeBytes)]
        [ProducesResponseType<PriceListImportAnalysisResDTO>(StatusCodes.Status200OK)]
        public async Task<IActionResult> AnalyzeImport(
            Guid id,
            [FromForm] IFormFile? file,
            CancellationToken cancellationToken)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });
            if (_importService is null) return StatusCode(StatusCodes.Status503ServiceUnavailable);
            if (file is null || file.Length == 0) return BadRequest(new { Message = "File is required." });

            await using var stream = file.OpenReadStream();
            return Ok(await _importService.AnalyzeAsync(id, file.FileName, stream, cancellationToken));
        }

        [HttpPost("{id:guid}/imports/{batchId:guid}/confirm")]
        [Authorize(Policy = Permissions.LibraryManage)]
        [ProducesResponseType<PriceListImportBatchResDTO>(StatusCodes.Status200OK)]
        public async Task<IActionResult> ConfirmImport(
            Guid id,
            Guid batchId,
            [FromBody] PriceListImportConfirmReqDTO request,
            CancellationToken cancellationToken)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });
            if (_importService is null) return StatusCode(StatusCodes.Status503ServiceUnavailable);

            return Ok(await _importService.ConfirmAsync(
                id,
                batchId,
                request.RowVersion,
                CurrentUserId.Value,
                cancellationToken));
        }

        [HttpGet("{id:guid}/imports")]
        [Authorize(Policy = Permissions.LibraryManage)]
        [ProducesResponseType<List<PriceListImportBatchResDTO>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> ListImports(
            Guid id,
            [FromQuery] int top = 20,
            CancellationToken cancellationToken = default)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });
            if (_importService is null) return StatusCode(StatusCodes.Status503ServiceUnavailable);

            return Ok(await _importService.ListAsync(id, top, cancellationToken));
        }
    }
}
